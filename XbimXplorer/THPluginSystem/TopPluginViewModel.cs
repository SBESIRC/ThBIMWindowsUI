using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using THBimEngine.Application;

namespace XbimXplorer.THPluginSystem
{
    class TopPluginViewModel : NotifyPropertyChangedBase
    {
        IEngineApplication engineApp;
        public ObservableCollection<TopPluginButton> TopPluginButtons { get; set; }
        public TopPluginViewModel(IEngineApplication engineApplication) 
        {
            engineApp = engineApplication;
            TopPluginButtons = new ObservableCollection<TopPluginButton>();
        }
        RelayCommand<Button> pluginButtonCommand;
        public ICommand PluginButtonCommond
        {
            get
            {
                if (pluginButtonCommand == null)
                    pluginButtonCommand = new RelayCommand<Button>((button) => PluginButtonCommand(button));
                return pluginButtonCommand;
            }
        }
        private void PluginButtonCommand(Button button)
        {
            if (button.DataContext == null)
                return;
            var plugin = button.DataContext as TopPluginButton;
            if (plugin == null || plugin.Command == null)
                return;
            plugin.Command.Execute(engineApp);

        }
    }
    class TopPluginButton 
    {
        public PluginButtonType ButtonType { get; }
        public string Content { get; }
        public string IconPath { get; }
        public int Order { get; }
        public IPluginCommand Command { get; }
        public TopPluginButton(PluginButtonType buttonType,string text,string iconPath,int order,IPluginCommand command) 
        {
            ButtonType = buttonType;
            Content = text;
            IconPath = iconPath;
            Order = order;
            Command = command;
        }
    }
}
