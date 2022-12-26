using System.Collections.Generic;
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
        ProjectFileViewModel projectFileView;
        private ShowFileLink linkModel;
        private bool haveReturnLink;
        public AddLinkModelUI(List<ShowProjectFile> showProjectFiles)
        {
            InitializeComponent();
            projectFileView = new ProjectFileViewModel(showProjectFiles);
            modelSelectMainGrid.DataContext = projectFileView;
        }
        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (modelDataGrid.SelectedItem == null)
            {
                MessageBox.Show("当前没有选中要链接的模型，无法进行后续操作，请选择要链接的模型后再进行后续操作");
                return;
            }
            var addLinkSet = new AddLinkSetUI(0,0,0,0,true);
            addLinkSet.Owner = this;
            if (addLinkSet.ShowDialog() == true) 
            {
                //设置成功，读取相应的信息
                var rotation = addLinkSet.GetInputData(out double x, out double y, out double z,out bool needReturn);
                var prjFile = modelDataGrid.SelectedItem as ShowProjectFile;
                linkModel = new ShowFileLink()
                {
                    LinkProject = prjFile,
                    LinkId = System.Guid.NewGuid().ToString(),
                    LinkProjectFileId = prjFile.ProjectFileId,
                    MoveX = x,
                    MoveY = y,
                    MoveZ = z,
                    State = 0,
                    RotainAngle = rotation,
                };
                haveReturnLink = needReturn;
                this.DialogResult = true;
                this.Close();
            }
        }
        public ShowFileLink GetLinkModel(out bool needReturn) 
        {
            needReturn = haveReturnLink;
            return linkModel;
        }
        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        
    }
    
}
