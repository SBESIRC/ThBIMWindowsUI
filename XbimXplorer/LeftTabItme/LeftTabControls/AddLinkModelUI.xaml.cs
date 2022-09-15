using System.Windows;
using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// AddLinkModelUI.xaml 的交互逻辑
    /// </summary>
    public partial class AddLinkModelUI : Window
    {
        private string projectRootPath { get; }
        ProjectFileViewModel projectFileView;
        private LinkModel linkModel;
        public AddLinkModelUI(string prjRootPath)
        {
            InitializeComponent();
            projectRootPath = prjRootPath;
            InitProjectViewModel();
        }
        private void InitProjectViewModel()
        {
            FileProject fileProject = new FileProject(projectRootPath);
            projectFileView = new ProjectFileViewModel(fileProject);
            modelSelectMainGrid.DataContext = projectFileView;
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (modelDataGrid.SelectedItem == null)
            {
                MessageBox.Show("当前没有选中要链接的模型，无法进行后续操作，请选择要链接的模型后再进行后续操作");
                return;
            }
            var addLinkSet = new AddLinkSetUI(0,0,0,0);
            if (addLinkSet.ShowDialog() == true) 
            {
                //设置成功，读取相应的信息
                var rotation = addLinkSet.GetInputData(out double x, out double y, out double z);
                linkModel = new LinkModel()
                {
                    MoveMatrix3D = XbimMatrix3D.CreateTranslation(x, y, z),
                    Project = modelDataGrid.SelectedItem as ProjectModel,
                    LinkState = "已链接",
                    RotainAngle = rotation,
                };
                this.DialogResult = true;
                this.Close();
            }
        }
        public LinkModel GetLinkModel() 
        {
            return linkModel;
        }
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        
    }
    
}
