using System.Windows;
using System.Windows.Controls;
using Xbim.Common.Geometry;
using Xbim.Presentation.XplorerPluginSystem;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// LinkUControl.xaml 的交互逻辑
    /// </summary>
    public partial class LinkUControl : UserControl, IXbimXplorerPluginWindow
    {
        private string currentPrjRootPath = "";
        private XplorerMainWindow mainWindow;
        private LinkViewModel linkViewModel;
        public string WindowTitle => "";

        public LinkUControl()
        {
            InitializeComponent();
            InitViewModel();
        }
        private void InitViewModel()
        {
            linkViewModel = new LinkViewModel();
            mainGrid.DataContext = linkViewModel;
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void btnAddLink_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(currentPrjRootPath)) 
            {
                currentPrjRootPath = SelectProjectRootPath();
            }
            if (string.IsNullOrEmpty(currentPrjRootPath)) 
            {
                MessageBox.Show("没有项目文件路径，无法进行后续操作");
                return;
            }
            var addLinkUI = new AddLinkModelUI(currentPrjRootPath);
            if (addLinkUI.ShowDialog() == true) 
            {
                //新增成功
                var linkRes = addLinkUI.GetLinkModel();
                AddLinkModel(linkRes);
            }
        }
        private string SelectProjectRootPath() 
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择项目所在文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    return "";
                }
                return dialog.SelectedPath;
            }
            return "";
        }

        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            this.mainWindow = mainWindow as XplorerMainWindow;
        }

        private void AddLinkModel(LinkModel linkModel) 
        {
            if (null == mainWindow)
                return;
            bool isAdd = true;
            foreach (var item in linkViewModel.AllLinkModel) 
            {
                if (item.Project.FileName == linkModel.Project.FileName)
                {
                    isAdd = false;
                    break;
                }
            }
            if (!isAdd)
                return;
            linkViewModel.AllLinkModel.Add(linkModel);
            
            mainWindow.LoadAnyModel(linkModel.Project.FileName, linkModel.MoveMatrix3D);
        }
        private void RemoveLinkModel(LinkModel linkModel) 
        {
            if (null == linkModel)
                return;
            linkViewModel.AllLinkModel.Remove(linkModel);
            mainWindow.RemoveModel(linkModel.Project.FileName);
        }

        private void changeLink_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridLink.SelectedItem == null)
                return; 
            var linkModel = dataGridLink.SelectedItem as LinkModel;
            var setUI = new AddLinkSetUI(linkModel.RotainAngle, linkModel.MoveMatrix3D.OffsetX,linkModel.MoveMatrix3D.OffsetY,linkModel.MoveMatrix3D.OffsetZ);
            if (setUI.ShowDialog() == true)
            {
                //修改成功
                var rotation = setUI.GetInputData(out double x, out double y, out double z);
                linkModel.LinkState = "已链接";
                linkModel.RotainAngle = rotation;
                linkModel.MoveMatrix3D = XbimMatrix3D.CreateTranslation(x, y, z);
                mainWindow.LoadAnyModel(linkModel.Project.FileName, linkModel.MoveMatrix3D);
            }
        }

        private void btnDelLink_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridLink.SelectedItem == null)
                return;
            var linkModel = dataGridLink.SelectedItem as LinkModel;
            RemoveLinkModel(linkModel);
        }
    }
}
