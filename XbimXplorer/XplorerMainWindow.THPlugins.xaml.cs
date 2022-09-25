using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using THBimEngine.Application;
using XbimXplorer.THPluginSystem;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        private PluginService pluginService;
        private LeftPluginViewModel leftPluginViewModel;
        private TopPluginViewModel topPluginViewModel;
        void InitTHPlugins() 
        {
            var assemblyPath = this.GetType().Assembly.Location.ToString();
            pluginService = new PluginService(this,new List<string> { assemblyPath });
            
            var currentDir = System.Environment.CurrentDirectory;
            var pluginsDir = Path.Combine(currentDir, "Plugins");
            if (Directory.Exists(pluginsDir)) 
            {
                var pluginDlls = Directory.GetFiles(pluginsDir, "*.dll", SearchOption.AllDirectories);
                foreach (var item in pluginDlls) 
                {
                    try
                    {
                        pluginService.PluginAdd(item);
                    }
                    catch (Exception ex) 
                    {
                        var msg = string.Format("插件{0}加载失败：{1}", item, ex.Message);
                        MessageBox.Show(msg, "插件加载", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            InitLeftPlugin();
            InitTopPlugin();
        }
        void InitLeftPlugin() 
        {
            leftPluginViewModel = new LeftPluginViewModel(this);
            foreach (var item in pluginService.LeftPluginSvrCaches)
            {
                var uInstance = Activator.CreateInstance(item.PType) as UserControl;
                var iApp = uInstance as IPluginApplicaton;
                if (iApp != null)
                    iApp.BindApplication(this);
                leftPluginViewModel.LeftTabItems.Add(new LeftTabItemBtn(item.PluginName, 200, uInstance));
            }
            leftTabControl.DataContext = leftPluginViewModel;
        }
        void InitTopPlugin() 
        {
            topPluginViewModel = new TopPluginViewModel(this);
            foreach (var item in pluginService.TopPluginSvrCaches)
            {
                var uInstance = Activator.CreateInstance(item.PType) as IPluginCommand;
                topPluginViewModel.TopPluginButtons.Add(new TopPluginButton(item.ButtonType,item.PluginName,item.IconPath,item.Order,uInstance));
            }
            topPluginGrid.DataContext = topPluginViewModel;

        }
        #region 左侧按钮相关事件
        LeftPluginMainUControl leftPluginMainUControl;
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tab = sender as TabControl;
            if (tab.SelectedItem != null)
            {
                var tabSelect = tab.SelectedItem as LeftTabItemBtn;
                tabSelect.PanelControl.Visibility = Visibility.Visible;
                var winHost = GetOrAddLeftHost();
                if (winHost.Child != null)
                {
                    var tempHost = winHost.Child as ElementHost;
                    tempHost.Child = null;
                    tempHost.Dispose();
                    winHost.Child = null;
                }
                ElementHost elementHost = new ElementHost();
                if (null == leftPluginMainUControl)
                    leftPluginMainUControl = new LeftPluginMainUControl(tabSelect.PanelControl);
                else
                    leftPluginMainUControl.SetNewUControl(tabSelect.PanelControl);
                elementHost.Child = leftPluginMainUControl;
                elementHost.Child.IsVisibleChanged += Child_IsVisibleChanged;
                winHost.Child = elementHost;
            }
        }
        private void Child_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
                return;
            var winHost = GetOrAddLeftHost();
            if (winHost.Child == null)
                return;
            var elemHost = (winHost.Child as ElementHost);
            if (elemHost.Child == null)
                return;
            bool isClose = leftTabControl.SelectedItem != null;
            elemHost.Child.IsVisibleChanged -= Child_IsVisibleChanged;
            elemHost.Child = null;
            elemHost.Dispose();
            winHost.Child = null;
            if (isClose)
                leftTabControl.SelectedItem = null;
        }
        WindowsFormsHost GetOrAddLeftHost()
        {
            var winFormName = "TempShowFormHost";
            WindowsFormsHost winHost = null;
            foreach (var item in mainGrid.Children)
            {
                if (item is WindowsFormsHost host)
                {
                    if (host.Name == winFormName)
                    {
                        winHost = host;
                    }
                }
            }
            if (winHost == null)
            {
                winHost = new WindowsFormsHost();
                winHost.Name = winFormName;
                winHost.Style = this.Resources["TempHostStyle"] as Style;
                mainGrid.Children.Add(winHost);
            }
            return winHost;
        }
        #endregion
    }
}
