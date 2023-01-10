using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using THBimEngine.Application;
using THBimEngine.Internal.ViewModel;

namespace THBimEngine.Internal.UI
{
    /// <summary>
    /// SelectEntityUI.xaml 的交互逻辑
    /// </summary>
    public partial class SelectEntityUI : Window
    {
        IEngineApplication engineApp;
        SelectEntityVM selectEntityVM;
        public SelectEntityUI(IEngineApplication engineApplication)
        {
            InitializeComponent();
            engineApp = engineApplication;
            InitVM();
            this.DataContext = selectEntityVM;
        }
        private void InitVM() 
        {
            selectEntityVM = new SelectEntityVM();
            if (null == engineApp || engineApp.CurrentDocument == null || engineApp.CurrentDocument.AllBimProjects.Count < 1)
                return;
            foreach (var item in engineApp.CurrentDocument.AllBimProjects) 
            {
                var id = item.ProjectIdentity;
                var showName = item.Name;
                selectEntityVM.AllFiles.Add(new FileFilterVM(id, showName));
            }
        }
        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            var indexs = GetIndexs();
            if (indexs.Count < 1)
                return;
            engineApp.ZoomEntitys(indexs);
        }

        private void btnShow_Click(object sender, RoutedEventArgs e)
        {
            var indexs = GetIndexs();
            if (indexs.Count < 1)
                return;
            engineApp.ShowEntityByIds(indexs);
        }
        private List<int> GetIndexs() 
        {
            List<int> indexs = new List<int>();
            var str = txtIndex.Text;
            if (string.IsNullOrEmpty(str))
                return indexs;
            var splite = str.Split(';').ToList();
            foreach (var item in splite)
            {
                if (int.TryParse(item, out int value))
                    indexs.Add(value);
            }
            return indexs;
        }

        private void btnIFCSelect_Click(object sender, RoutedEventArgs e)
        {
            var indexs = GetIndexsFromIFC();
            if (indexs.Count < 1)
            {
                MessageBox.Show("在IFC中没有找到相应的实体");
                return;
            }
            engineApp.ZoomEntitys(indexs);
        }

        private void btnIFCShow_Click(object sender, RoutedEventArgs e)
        {
            var indexs = GetIndexsFromIFC();
            if (indexs.Count < 1)
            {
                MessageBox.Show("在IFC中没有找到相应的实体");
                return;
            }
            engineApp.ShowEntityByIds(indexs);
        }
        private List<int> GetIndexsFromIFC()
        {
            var indexs = new List<int>();
            var ifcLables = GetIFCLables();
            if (ifcLables.Count < 1)
                return indexs;
            var selectIfcs = selectEntityVM.AllFiles.Where(c => c.IsChecked == true).Select(c => c.FileName).ToList();
            if (selectIfcs.Count < 1)
                return indexs;
            foreach (var prj in engineApp.CurrentDocument.MeshEntiyRelationIndexs) 
            {
                if (!selectIfcs.Contains(prj.Value.ProjectId))
                    continue;
                if (!ifcLables.Contains(prj.Value.ProjectEntityId))
                    continue;
                indexs.Add(prj.Key);
            }
            return indexs;
        }
        private List<string> GetIFCLables()
        {
            var lables = new List<string>();
            var str = txtEntitys.Text;
            if (string.IsNullOrEmpty(str))
                return lables;
            lables = str.Split(';').ToList();
            return lables;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (null == engineApp || null == engineApp.CurrentDocument)
                return;
            engineApp.ShowEntityByIds(engineApp.CurrentDocument.MeshEntiyRelationIndexs.Keys.ToList());
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
