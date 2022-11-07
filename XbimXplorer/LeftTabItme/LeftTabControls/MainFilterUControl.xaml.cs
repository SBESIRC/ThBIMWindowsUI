using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using THBimEngine.Application;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// MainFilterUControl.xaml 的交互逻辑
    /// </summary>
    [EnginePlugin(PluginButtonType.Button, 0,"过\r\n滤","")]
    public partial class MainFilterUControl : UserControl, IPluginApplicaton
    {
        private IEngineApplication engineApp;
        FilterViewModel filterViewModel;
        public MainFilterUControl()
        {
            InitializeComponent();
        }
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectItems = floorDGrid.SelectedItems.Cast<FloorFilterViewModel>();
            if (selectItems.Count() < 1 || engineApp.CurrentDocument == null)
                return;
            foreach (var item in filterViewModel.AllFloorFilters)
            {
                if (selectItems.Any(c => c == item))
                {
                    item.IsChecked = true;
                }
                else
                {
                    item.IsChecked = false;
                }
            }
            filterViewModel.UpdataFloorShowIds();
            filterViewModel.ShowFilterResult();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            floorDGrid.SelectAll();
        }

        public void BindApplication(IEngineApplication engineApplication)
        {
            engineApp = engineApplication;
            filterViewModel = new FilterViewModel(engineApp);
            mainGrid.DataContext = filterViewModel;
            ShowFilterByCurrentDocument();
            engineApplication.DocumentManager.SelectDocumentChanged += EngineApplication_SelectDocumentChanged;
            if(engineApplication.DocumentManager.CurrentDocument != null)
                engineApplication.DocumentManager.CurrentDocument.DocumentChanged += EngineApplication_DocumentChanged;
        }

        private void EngineApplication_DocumentChanged(object sender, EventArgs e)
        {
            ShowFilterByCurrentDocument();
        }
        private void EngineApplication_SelectDocumentChanged(object sender, EventArgs e)
        {
            if (engineApp.DocumentManager.CurrentDocument != null)
                engineApp.DocumentManager.CurrentDocument.DocumentChanged += EngineApplication_DocumentChanged;
            ShowFilterByCurrentDocument();

        }
        private void ShowFilterByCurrentDocument() 
        {
            filterViewModel.UpdataFilterByCurrentDocument(engineApp.CurrentDocument);
        }
    }
}
