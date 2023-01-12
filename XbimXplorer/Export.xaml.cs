using Xbim.Ifc;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using XbimXplorer.Extensions.ModelMerge;

namespace XbimXplorer
{
    /// <summary>
    /// Export.xaml 的交互逻辑
    /// </summary>
    public partial class Export : Window
    {
        ExportViewModel exportViewModel;
        public Export()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            exportViewModel = new ExportViewModel((this.Owner as XplorerMainWindow).CurrentDocument);
            mainGrid.DataContext = exportViewModel;
            if (exportViewModel.AllProjects.Count == 0)
            {
                MessageBox.Show("未检测到当前环境主体存在，无法导出！", "提示", MessageBoxButton.OK);
                this.Close();
            }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = ".ifc";
            dlg.OverwritePrompt = true;
            dlg.Filter = "ifc files |*.ifc";
            dlg.Title = "Save the IFC File as...";
            dlg.ValidateNames = true;
            if (dlg.ShowDialog() == true)
            {
                var path = dlg.FileName;
                var bimProjects = exportViewModel.AllProjects.Where(o => o.IsChecked.Value);
                if (bimProjects.Any())
                {
                    var projects = bimProjects.Select(o => o.Project.SourceProject);
                    var ifcProjects = projects.OfType<IfcStore>().ToList();
                    var suProjects = projects.OfType<ThSUProjectData>().ToList();
                    if (ifcProjects.Count == 1 &&  ifcProjects.First() == exportViewModel.AllProjects.First().Project.SourceProject)
                    {
                        //一个主体 多个SU
                        THModelMergeService modelMergeService = new THModelMergeService();
                        var mergeIfc = modelMergeService.ModelMerge(ifcProjects.First(), suProjects);
                        if (mergeIfc != null)
                        {
                            mergeIfc.SaveAs(path);
                            mergeIfc.Dispose();
                        }
                        MessageBox.Show("已成功导出！", "提示", MessageBoxButton.OK);
                    }
                    else if (ifcProjects.Count > 0)
                    {
                        //一个主体IFC + 多个SU IFC
                        MessageBox.Show("暂时只支持主体IFC与多个SU的导出！", "提示", MessageBoxButton.OK);
                    }
                    else
                    {
                        MessageBox.Show("暂时只支持主体IFC与多个SU的导出！", "提示", MessageBoxButton.OK);
                    }
                }
                else
                {
                    MessageBox.Show("导出IFC需要至少选择一个主体！", "提示", MessageBoxButton.OK);
                }
                this.Close();
            }
        }
    }
}
