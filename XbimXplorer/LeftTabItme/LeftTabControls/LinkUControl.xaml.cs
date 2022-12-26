using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using THBimEngine.Application;
using THBimEngine.Domain;
using XbimXplorer.Project;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// LinkUControl.xaml 的交互逻辑
    /// </summary>
    [EnginePlugin(PluginButtonType.Button, 1, "外\r\n链", "")]
    public partial class LinkUControl : UserControl, IPluginApplicaton
    {
        private ProjectFileManager projectFileManager;
        private IEngineApplication engineApp;
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
        private void btnAddLink_Click(object sender, RoutedEventArgs e)
        {
            if (engineApp.CurrentDocument == null || engineApp.CurrentDocument.ProjectFile == null)
            {
                MessageBox.Show("没有选中任何项目，请在项目管理中选择项目后再进行后续操作","操作提醒",MessageBoxButton.OK,MessageBoxImage.Warning);
                return;
            }
            var allFiles = projectFileManager.GetProjectFiles(engineApp.CurrentDocument.ProjectFile.PrjId);
            if (null == allFiles || allFiles.Count < 1)
            {
                MessageBox.Show("项目中没有任何文件，无法进行外链，请再项目管理中加入后再进行后续操作", "操作提醒", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var currentMainId = engineApp.CurrentDocument.ProjectFile.ProjectFileId;
            allFiles = allFiles.Where(c => c.ProjectFileId != currentMainId).ToList();
            var addLinkUI = new AddLinkModelUI(allFiles);
            addLinkUI.Owner = engineApp as Window;
            if (addLinkUI.ShowDialog() == true) 
            {
                //新增成功
                var linkRes = addLinkUI.GetLinkModel(out bool haveReturnLink);
                linkRes.FromLinkId = "";
                linkRes.ProjectFileId = engineApp.CurrentDocument.ProjectFile.ProjectFileId;
                AddLinkModel(linkRes, haveReturnLink);
            }
        }
        private void AddLinkModel(ShowFileLink linkModel, bool haveReturnLink) 
        {
            if (null == engineApp)
                return;
            if (!projectFileManager.AddFileLink(linkModel, haveReturnLink))
                return;
            linkViewModel.AllLinkModel.Add(linkModel);
            OpenLinkModel(linkModel);
        }
        private void RemoveLinkModel(ShowFileLink linkModel,bool rmList) 
        {
            if(rmList)
                linkViewModel.AllLinkModel.Remove(linkModel);
            engineApp.RemoveProjectFormCurrentDocument(linkModel.LinkId);
        }

        private void changeLink_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridLink.SelectedItem == null)
                return; 
            var linkModel = dataGridLink.SelectedItem as ShowFileLink;
            var setUI = new AddLinkSetUI(linkModel.RotainAngle, linkModel.MoveX, linkModel.MoveY,linkModel.MoveZ,false);
            if (setUI.ShowDialog() == true)
            {
                //判断是否修改，如果有修改，修改数据库，并重新载入文件
                var rotation = setUI.GetInputData(out double x, out double y, out double z,out bool needReturn);
                bool haveChange = Math.Abs(rotation - linkModel.RotainAngle)>0.0001 
                    || Math.Abs(x - linkModel.MoveX) > 0.0001
                    || Math.Abs(y - linkModel.MoveY) > 0.0001
                    || Math.Abs(z - linkModel.MoveZ) > 0.0001;
                if (!haveChange)
                    return;
                linkModel.RotainAngle = rotation;
                linkModel.MoveX = x;
                linkModel.MoveY = y;
                linkModel.MoveZ = z;
                if (projectFileManager.UpdateFileLink(linkModel))
                    OpenLinkModel(linkModel);
            }
        }
        private void OpenLinkModel(ShowFileLink linkModel) 
        {
            if (null == linkModel.LinkProject.OpenFile || linkModel.State > 0)
                return;
            var matrix3D = linkModel.GetLinkMatrix3D;
            projectFileManager.FileLocalPathCheckAndDownload(linkModel.LinkProject.OpenFile,true);
            var openParameter = new ProjectParameter()
            {
                OpenFilePath = linkModel.LinkProject.OpenFile.FileLocalPath,
                ProjectId = linkModel.LinkId,
                Matrix3D = matrix3D,
                Major = linkModel.LinkProject.Major,
                Source = linkModel.LinkProject.ApplcationName,
                SourceShowName = linkModel.LinkProject.ShowSourceName,
            };
            engineApp.LoadFileToCurrentDocument(openParameter);
        }
        private void btnDelLink_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridLink.SelectedItem == null)
                return;
            var linkModel = dataGridLink.SelectedItem as ShowFileLink;
            if (null == linkModel)
                return;
            var res = MessageBox.Show("确定要删除吗？，改过程是不可逆的!是否继续操作？", "操作提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;
            if (!projectFileManager.ChangeLinkState(linkModel, true, true))
                return;
            RemoveLinkModel(linkModel,true);
        }
        public void BindApplication(IEngineApplication engineApplication)
        {
            engineApp = engineApplication;
            var uId = engineApplication.GetCurrentUserIdName(out string userName, out string loginLocation);
            projectFileManager = new ProjectFileManager(uId, userName, loginLocation);
            engineApplication.DocumentManager.SelectDocumentChanged += DocumentManager_SelectDocumentChanged;
        }
        private void DocumentManager_SelectDocumentChanged(object sender, System.EventArgs e)
        {
            ShowCurrentDocumentMainFileLinks(engineApp.DocumentManager.CurrentDocument);
        }
        private void ShowCurrentDocumentMainFileLinks(THDocument document) 
        {
            linkViewModel.ClearData();
            if (document == null || document.ProjectFile == null)
                return;
            if (string.IsNullOrEmpty(document.ProjectFile.ProjectFileId))
                return;
            //获取当前主项目的外链信息，如果有多个Document来回切换会有一个问题(目前没有切换Document的功能)
            //如果远端数据有更新，这里外链显示刷新到了远端，但模型显示没有刷新到远端，
            //要想模型显示和远端一致，需要在项目管理中重新双击项目载入
            var hisLinks = projectFileManager.GetMainFileLinkInfo(new List<string> { document.ProjectFile.ProjectFileId });
            foreach(var item in hisLinks)
                linkViewModel.AllLinkModel.Add(item);
        }

        private void dataGridLink_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DataGrid dGrid = (DataGrid)sender;
            if (dGrid == null)
                return;
            var rowData = dGrid.SelectedItem as ShowFileLink;
            if (null == rowData)
                return;
            ContextMenu aMenu = new ContextMenu();
            MenuItem editMenu = new MenuItem();
            editMenu.Header = "修改";
            editMenu.Click += changeLink_Click;
            aMenu.Items.Add(editMenu);
            if (rowData.State < 1)
            {
                //已装载，显示卸载
                MenuItem loadMenu = new MenuItem();
                loadMenu.Header = "卸载";
                loadMenu.Click += UnLoadMenu_Click;
                aMenu.Items.Add(loadMenu);
            }
            else 
            {
                //已卸载，显示装载
                MenuItem unLoadMenu = new MenuItem();
                unLoadMenu.Header = "装载";
                unLoadMenu.Click += LoadMenu_Click;
                aMenu.Items.Add(unLoadMenu);
            }
            MenuItem deleteMenu = new MenuItem();
            deleteMenu.Header = "删除";
            deleteMenu.Click += btnDelLink_Click;
            aMenu.Items.Add(deleteMenu);
            dGrid.ContextMenu = aMenu;
        }

        private void UnLoadMenu_Click(object sender, RoutedEventArgs e)
        {
            var linkModel = dataGridLink.SelectedItem as ShowFileLink;
            if (null == linkModel)
                return;
            if (!projectFileManager.ChangeLinkState(linkModel, false, false))
                return;
            linkModel.State = 1;
            RemoveLinkModel(linkModel,false);
        }

        private void LoadMenu_Click(object sender, RoutedEventArgs e)
        {
            var linkModel = dataGridLink.SelectedItem as ShowFileLink;
            if (null == linkModel)
                return;
            if (!projectFileManager.ChangeLinkState(linkModel, true, false))
                return;
            linkModel.State = 0;
            OpenLinkModel(linkModel);
        }
    }
}
