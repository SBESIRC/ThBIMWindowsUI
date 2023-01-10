using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using THBimEngine.Common;
using THBimEngine.Domain;
using THBimEngine.HttpService;
using XbimXplorer.Project;

namespace XbimXplorer
{
    /// <summary>
    /// ProjectManage.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectManager : Window
    {
        ProjectFileManager projectFileManager;
        static ProjectVM projectVM =null;
        OperateType operateType;
        ShowProjectFile selectProjectFile;
        UserInfo loginUser;
        ShowProject pProject;
        ShowProject subProject;
        public ProjectManager(UserInfo user)
        {
            InitializeComponent();
            loginUser = user;
            operateType = OperateType.Close;
            projectFileManager = new ProjectFileManager(user);
            InitUserProjects();
        }
        public OperateType GetOperateType() 
        {
            return operateType;
        }
        public ShowProjectFile GetOpenModel() 
        {
            return selectProjectFile;
        }
        void InitUserSelectProject() 
        {
            pProject = null;
            subProject = null;
            if(projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            subProject = projectVM.SelectProject;
            pProject = projectVM.GetParentPrject(subProject);
        }
        public ShowProject GetSelectPrjSubPrj(out ShowProject subProject, out List<ShowProject> allSubPrjs, out string loaclPrjPath) 
        {
            InitUserSelectProject();
            ShowProject pProject = null;
            subProject = null; 
            allSubPrjs = new List<ShowProject>();
            loaclPrjPath = string.Empty;
            var selectPrj = projectVM.SelectProject;
            if (selectPrj == null)
                return pProject;
            allSubPrjs = projectVM.GetOneProject(selectPrj);
            pProject = allSubPrjs.First();
            if (selectPrj.IsChild)
            {
                subProject = selectPrj;
            }
            allSubPrjs.Remove(pProject);
            loaclPrjPath = ProjectCommon.GetParentProjectPath(pProject, loginUser.LoginLocation, true);
            return pProject;
        }
        private void InitUserProjects()
        {
            if (null == projectVM)
            {
                var userPojects = projectFileManager.ProjectDBHelper.GetUserProjects(loginUser.PreSSOId);
                projectVM = new ProjectVM(userPojects, projectFileManager);
            }
            else 
            {
                projectVM.ChangeSelectSubProject();
            }
            this.DataContext = projectVM;
        }

        private void btnUploadIFC_Click(object sender, RoutedEventArgs e)
        {
            SelectAndUploadFile("IFC");
        }
        private void SelectAndUploadFile(string type)
        {
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            InitUserSelectProject();
            var selectPath = new SelectUploadFile(type, projectVM.MajorNames, true);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() != true)
                return;
            var filePath = selectPath.GetSelectResult(out string major);
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var hisPrj = projectFileManager.ProjectFileDBHelper.GetHisProjectFile(pProject.PrjId, subProject.PrjId, major, type, fileName, "", 1);
                if (hisPrj != null)
                {
                    var msg = string.Format("项目文件名称【{0}】,已经存在，且作废了，无法添加同名称的，请到历史记录中激活后使用更新功能继续操作", fileName);
                    MessageBox.Show(msg, "操作提醒", MessageBoxButton.OK);
                    return;
                }
                hisPrj = projectFileManager.ProjectFileDBHelper.GetHisProjectFile(pProject.PrjId, subProject.PrjId, major, type, fileName, "", 0);
                if (hisPrj != null)
                {
                    var msg = string.Format("项目文件名称【{0}】,已经存在，请选中文件右键更新", fileName);
                    MessageBox.Show(msg, "操作提醒", MessageBoxButton.OK);
                    return;
                }
                lableRemind.Content = "正在进行相应的操作（包含上传文件）,请耐心等待...";
                gridRemind.Visibility = Visibility.Visible;
                Refresh();
                //第一步检查数据，上传文件是否是同名，如果是同名，则走更新流程
                //再检查是否是已经作废的数据，如果是已经作废的数据，则不允许
                var res = projectFileManager.AddFileToProject(pProject, subProject, filePath, major, type, true);
                if (res != true)
                    return;
                //操作成功
                projectVM.ChangeSelectSubProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("上传失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally 
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
            }
            
        }
        private void btnAddSUFile_Click(object sender, RoutedEventArgs e)
        {
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            InitUserSelectProject();
            var selectPath = new SelectUploadFile("SU", projectVM.MajorNames, false);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() != true)
                return;
            var fileName = selectPath.GetSelectResult(out string major);
            if (string.IsNullOrEmpty(fileName))
                return;
            var path = ProjectCommon.GetProjectSubDir(pProject, subProject,loginUser.LoginLocation, major, "SU", true);
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                var hisPrj = projectFileManager.ProjectFileDBHelper.GetHisProjectFile(pProject.PrjId, subProject.PrjId, major, "SU", fileName, "", 1);
                if (hisPrj != null)
                {
                    var msg = string.Format("项目文件名称【{0}】,已经存在，且作废了，无法添加同名称的，请到历史记录中激活后使用更新功能继续操作", fileName);
                    MessageBox.Show(msg, "操作提醒", MessageBoxButton.OK);
                    return;
                }
                hisPrj = projectFileManager.ProjectFileDBHelper.GetHisProjectFile(pProject.PrjId, subProject.PrjId, major, "SU", fileName, "", 0);
                if (hisPrj != null)
                {
                    var msg = string.Format("项目文件名称【{0}】,已经存在，请选中文件右键更新", fileName);
                    MessageBox.Show(msg, "操作提醒", MessageBoxButton.OK);
                    return;
                }
                lableRemind.Content = "正在进行相应的操作（包含上传文件）,请耐心等待...";
                gridRemind.Visibility = Visibility.Visible;
                Refresh();
                var currentDir = System.Environment.CurrentDirectory;
                var templatePath = Path.Combine(currentDir, "Template\\THSKPTemplate_S_2020.skp");
                path = Path.Combine(path, fileName + ".skp");
                
                File.Copy(templatePath, path, true);
                var res = projectFileManager.AddFileToProject(pProject, subProject, path, major, "SU", false);
                if (res != true)
                    return;
                //操作成功
                projectVM.ChangeSelectSubProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("添加失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
            }
        }
        private void btnUploadYDB_Click(object sender, RoutedEventArgs e)
        {
            SelectAndUploadFile("YDB");
        }

        private void Row_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == null)
                return;
            var dGridRow = sender as DataGridRow;
            if (dGridRow == null)
                return;
            var fileInfo = dGridRow.DataContext as ShowProjectFile;
            CheckLocalFileAndOpen(fileInfo);
        }
        private void PrjSubPrjRow_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == null)
                return;
            var dGridRow = sender as DataGridRow;
            if (dGridRow == null)
                return;
            var prjInfo = dGridRow.DataContext as ShowProject;
            if (null == prjInfo || prjInfo.IsChild)
                return;
            prjInfo.IsExpand = !prjInfo.IsExpand;
        }
        private void CheckLocalFileAndOpen(ShowProjectFile fileInfo,bool openFile =true) 
        {
            if (fileInfo == null)
                return;
            try
            {
                lableRemind.Content = "正在检查项目相应的文件（包含下载）,请耐心等待...";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
                var checkMD5 = true;
                if (fileInfo.ApplcationName == EApplcationName.SU) 
                {
                    checkMD5 = (string.IsNullOrEmpty(fileInfo.OccupyId) || fileInfo.OccupyId != loginUser.UserLogin.Username);
                }
                //检查并下载文件
                foreach (var item in fileInfo.FileInfos) 
                {
                    if (!item.NeedDownload)
                        continue;
                    projectFileManager.FileLocalPathCheckAndDownload(item, checkMD5);
                }
                if (fileInfo.ApplcationName == EApplcationName.SU)
                {
                    projectFileManager.FileLocalPathCheckAndDownload(fileInfo.MainFile, checkMD5);
                    if(File.Exists(fileInfo.MainFile.FileLocalPath))
                        File.SetAttributes(fileInfo.MainFile.FileLocalPath, FileAttributes.Normal);
                    if (openFile) 
                    {
                        if (!File.Exists(fileInfo.MainFile.FileLocalPath))
                            return;
                        if (!checkMD5)
                            CheckLocalFileServices.Instance.AddCheckFile(fileInfo);
                        OpenFile(fileInfo.MainFile.FileLocalPath, checkMD5);
                    }
                }
                else
                {
                    if (fileInfo.OpenFile == null || !File.Exists(fileInfo.OpenFile.FileLocalPath))
                        return;
                    //获取最新的外链信息
                    fileInfo.FileLinks = new List<ShowFileLink>();
                    var hisLinks = projectFileManager.GetMainFileLinkInfo(new List<string> { fileInfo.ProjectFileId });
                    foreach (var item in hisLinks) 
                    {
                        if (null == item.LinkProject )
                            continue;
                        if (item.LinkProject.OpenFile == null) 
                        {
                            if(item.LinkProject.ApplcationName != EApplcationName.SU)
                                continue;
                            //su加入本地文件信息
                            var temp = item.LinkProject.MainFile.Clone() as FileDetail;
                            temp.CanOpen = true;
                            temp.IsMainFile = false;
                            var localDir = Path.GetDirectoryName(temp.FileLocalPath);
                            var fileName = Path.GetFileNameWithoutExtension(temp.FileLocalPath);
                            temp.FileRealName = string.Format("{0}.ifc", fileName);
                            temp.FileLocalPath = Path.Combine(localDir, temp.FileRealName);
                            temp.FileMD5 = "";
                            temp.FileDownloadPath = "";
                            item.LinkProject.OpenFile = temp;
                        }  
                        //添加到信息中，并检查外链所需要的文件
                        fileInfo.FileLinks.Add(item);
                        checkMD5 = item.LinkProject.ApplcationName != EApplcationName.SU;
                        projectFileManager.FileLocalPathCheckAndDownload(item.LinkProject.OpenFile, checkMD5);
                    }
                    selectProjectFile = fileInfo;
                    operateType = OperateType.OpenFile;
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("添加失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
            }
        }
        private void OpenFile(string filePath,bool isReadOnly) 
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            if (!File.Exists(filePath))
                return;
            var attributes = File.GetAttributes(filePath);
            if (isReadOnly)
            {
                File.SetAttributes(filePath, attributes | FileAttributes.ReadOnly);
            }
            else 
            {
                File.SetAttributes(filePath, FileAttributes.Normal);
            }
            Process.Start(filePath);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            operateType = OperateType.Close;
            this.Close();
        }
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            operateType = OperateType.Close;
            this.Close();
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            if (null != projectVM)
                projectVM.FilterProject(Search_Text.Text.Trim());
        }

        private void Search_Text_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter)
                return;
            if (null != projectVM)
                projectVM.FilterProject(Search_Text.Text.Trim());
        }
        private void DeleteFile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var selectItem = menuItem.CommandParameter as ShowProjectFile;
            if (null == selectItem)
                return;
            InitUserSelectProject();
            try
            {
                lableRemind.Content = "正在进行相应的操作,请耐心等待...";
                gridRemind.Visibility = Visibility.Visible;
                Refresh();
                //检查并下载文件
                CheckLocalFileServices.Instance.RemoveCheckFile(selectItem.ProjectFileId);
                var res = projectFileManager.ProjectFileDelete(pProject, subProject, selectItem);
                if (res != true)
                    return;
                projectVM.ChangeSelectSubProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("作废失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
            }
        }
        private void UpdateFile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var selectItem = menuItem.CommandParameter as ShowProjectFile;
            if (selectItem == null || string.IsNullOrEmpty(selectItem.MainFile.FileLocalPath))
                return;
            var type = selectItem.ApplcationName.ToString();
            var selectPath = new SelectUploadFile(type, new List<string> { selectItem.MajorName}, true);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() != true)
                return;
            var filePath = selectPath.GetSelectResult(out string major);
            try
            {
                lableRemind.Content = "正在进行相关的操作（包含文件上传）,请耐心等待...";
                gridRemind.Visibility = Visibility.Visible;
                Refresh();
                if (!projectFileManager.UpdateProjectFile(selectItem, filePath))
                    return; 
                CheckLocalFileServices.Instance.RemoveCheckFile(selectItem.ProjectFileId);
                projectVM.ChangeSelectSubProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("更新失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
            }
        }
        private void CheckAndUpdateFile(object sender, RoutedEventArgs e) 
        {
            var menuItem = sender as MenuItem;
            var selectItem = menuItem.CommandParameter as ShowProjectFile;
            if (selectItem == null || string.IsNullOrEmpty(selectItem.MainFile.FileLocalPath))
                return;
            try
            {
                lableRemind.Content = "正在进行相关的操作（包含文件上传）,请耐心等待...";
                gridRemind.Visibility = Visibility.Visible;
                Refresh();
                var res = CheckLocalFileServices.Instance.ForceUpdateProjectFile(selectItem);
                if (!string.IsNullOrEmpty(res)) 
                {
                    MessageBox.Show(res, "操作提醒", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (!res.Contains("成功"))
                        return;
                }
                projectVM.ChangeSelectSubProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("上传失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
            }
            
        }
        private void SyncProjectFile_Click(object sender, RoutedEventArgs e)
        {
            var selectItem = prjDGrid.SelectedItem as ShowProject;
            if (selectItem == null)
                return;
            //进行耗时操作提醒
            string msg = selectItem.IsChild ? "当前选中的为子项目，同步文件时将该子项的文件同步，" : "当前选中的为项目，同步文件时将所有子项的文件一起同步，";
            msg = string.Format("{0}如果文件比较多或网络延迟比较高，可能会需要比较长的时间，是否继续操作？", msg);
            var res = MessageBox.Show(msg, "操作提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;
            bool isTrue = true;
            
            var downloadErrorFiles = new Dictionary<string, string>();
            var delErrorFiles = new Dictionary<string, string>();
            try
            {
                lableRemind.Content = "正在检查本机的文件并下载数据,请耐心等待...";
                gridRemind.Visibility = Visibility.Visible;
                Refresh();
                InitUserSelectProject();
                //移除文件变化监听
                foreach (var fileModel in projectVM.subProjectAllFileModels)
                {
                    foreach (var item in fileModel.FileInfos)
                    {
                        if (string.IsNullOrEmpty(item.FileDownloadPath))
                            continue;
                        if (fileModel.ApplcationName == EApplcationName.SU)
                        {
                            CheckLocalFileServices.Instance.RemoveCheckFile(fileModel.ProjectFileId);
                        }
                    }
                }
                //先刷新当前子项
                projectVM.ChangeSelectSubProject();
                //获取想选中子项,文件夹下的所有文件，为了减少下载量，不会直接将文件删除，先判断是否需要删除
                //1、用不到的文件，或已经删除的文件 2、和服务器不一致版本的文件都删除
                var childDir = ProjectCommon.GetSubProjectPath(pProject, subProject, loginUser.LoginLocation, true);
                var delFiles = FileHelper.GetDirectoryFiles(childDir, true,true);
                foreach (var fileModel in projectVM.subProjectAllFileModels) 
                {
                    foreach (var item in fileModel.FileInfos) 
                    {
                        if (string.IsNullOrEmpty(item.FileDownloadPath))
                            continue;
                        if (fileModel.ApplcationName == EApplcationName.SU)
                        {
                            CheckLocalFileServices.Instance.RemoveCheckFile(fileModel.ProjectFileId);
                        }
                        if (fileModel.ApplcationName != EApplcationName.SU && !item.NeedDownload)
                            continue;
                        var localPath = item.FileLocalPath.ToUpper();
                        try
                        {
                            projectFileManager.FileLocalPathCheckAndDownload(item, true);
                            //已经处理过，不需要再删除的文件
                            delFiles = delFiles.Where(c => c != localPath).ToList();
                        }
                        catch(Exception ex)
                        {
                            //文件处理失败，需要删除
                            downloadErrorFiles.Add(item.FileLocalPath, ex.Message);
                        }
                    }
                }
                if (delFiles.Count > 0) 
                {
                    lableRemind.Content = "删除多余的文件,请耐心等待...";
                    Refresh();
                    foreach (var item in delFiles) 
                    {
                        try 
                        {
                            File.Delete(item);
                        }
                        catch (Exception ex)
                        {
                            delErrorFiles.Add(item, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isTrue = false;
                MessageBox.Show(string.Format("同步失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally 
            {
                lableRemind.Content = "";
                gridRemind.Visibility = Visibility.Collapsed;
                Refresh();
                if (isTrue)
                {
                    if (downloadErrorFiles.Count > 0 || delErrorFiles.Count > 0)
                    {
                        MessageBox.Show(string.Format("同步结束，有文件操作是吧，删除失败{0},下载失败{1}", delErrorFiles.Count,downloadErrorFiles.Count), "操作提醒", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else 
                    {
                        MessageBox.Show("同步完成", "操作提醒", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

        }
        public void Refresh()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new DispatcherOperationCallback(delegate (object f)
                {
                    ((DispatcherFrame)f).Continue = false;
                    return null;
                }
                    ), frame);
            Dispatcher.PushFrame(frame);
        }

        private void prjDGrid_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //项目右键按钮处理
            DataGrid dGrid = (DataGrid)sender;
            if (dGrid == null)
                return;
            var rowData = dGrid.SelectedItem as ShowProject;
            if (null == rowData || !rowData.IsChild)
                return;
            ContextMenu aMenu = new ContextMenu();
            MenuItem syncMenu = new MenuItem();
            syncMenu.Header = "同步文件";
            syncMenu.Click += SyncProjectFile_Click;
            aMenu.Items.Add(syncMenu);
            prjDGrid.ContextMenu = aMenu;
        }
        private void ProjectFile_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e) 
        {
            DataGrid dGrid = (DataGrid)sender;
            if (dGrid == null)
                return;
            dGrid.ContextMenu = null;
            //这个时候datagrid的选中项还没有改变，不能通过选中项来处理
            // 获取坐标
            Point p = e.GetPosition((ItemsControl)sender);
            //  通过指定 Point 返回命中测试的最顶层 Visual 对象。                                               
            HitTestResult htr = VisualTreeHelper.HitTest((ItemsControl)sender, p);
            TextBlock o = htr.VisualHit as TextBlock;
            if (o != null)
            {
                DataGridRow dgr = GetParentObject<DataGridRow>(o) as DataGridRow;
                dgr.Focus();
                dgr.IsSelected = true;
            }
            var rowData = dGrid.SelectedItem as ShowProjectFile;
            if (null == rowData)
                return;
            ContextMenu contextMenu = new ContextMenu();
            if (rowData.ApplcationName == EApplcationName.SU) 
            {
                //只有占用后才能进行修改操作
                if (string.IsNullOrEmpty(rowData.OccupyId))
                {
                    //可以占用
                    MenuItem occupyMenu = new MenuItem();
                    occupyMenu.Header = "设置为占用状态";
                    occupyMenu.Click += OccupyMenu_Click;
                    occupyMenu.CommandParameter = rowData;
                    contextMenu.Items.Add(occupyMenu);
                }
                else if (rowData.OccupyId == loginUser.UserLogin.Username)
                {
                    //自己占用的
                    MenuItem unOccupyMenu = new MenuItem();
                    unOccupyMenu.Header = "设置为共享状态";
                    unOccupyMenu.Click += UnOccupyMenu_Click;
                    unOccupyMenu.CommandParameter = rowData;
                    contextMenu.Items.Add(unOccupyMenu);
                    MenuItem updateMenu = new MenuItem();
                    updateMenu.Header = "更新";
                    updateMenu.Click += UpdateFile_Click;
                    updateMenu.CommandParameter = rowData;
                    contextMenu.Items.Add(updateMenu);
                    MenuItem deleteMenu = new MenuItem();
                    deleteMenu.Header = "作废";
                    deleteMenu.Click += DeleteFile_Click;
                    deleteMenu.CommandParameter = rowData;
                    contextMenu.Items.Add(deleteMenu);
                    MenuItem uploadMenu = new MenuItem();
                    uploadMenu.Header = "上传服务器";
                    uploadMenu.Click += CheckAndUpdateFile;
                    uploadMenu.CommandParameter = rowData;
                    contextMenu.Items.Add(uploadMenu);
                    if (File.Exists(rowData.MainFile.FileLocalPath))
                        File.SetAttributes(rowData.MainFile.FileLocalPath, FileAttributes.Normal);
                }
                else 
                {
                    //别人占用的,没有任何修改的权限
                }
            }
            if (contextMenu.Items.Count < 1)
                return;
            dGrid.ContextMenu = contextMenu;
        }

        private void UnOccupyMenu_Click(object sender, RoutedEventArgs e)
        {
            //结束占用，如果是SU,需要检查一遍本机文件和远端文件是否需要上传
            var menuItem = sender as MenuItem;
            var selectItem = menuItem.CommandParameter as ShowProjectFile;
            if (selectItem == null || string.IsNullOrEmpty(selectItem.MainFile.FileLocalPath))
                return;
            try
            {
                bool canOccupied = true;
                if (File.Exists(selectItem.MainFile.FileLocalPath))
                    canOccupied = !FileHelper.IsOccupied(selectItem.MainFile.FileLocalPath);
                if (!canOccupied)
                {
                    string msg = string.Format("本机文件【{0}】还在占用中，请先关闭打开的文件", selectItem.ShowFileName);
                    MessageBox.Show(msg, "操作提醒", MessageBoxButton.OK);
                    return;
                }
                var checkPrjId = selectItem.ProjectFileId;
                projectVM.ChangeSelectSubProject();
                var newPrjInfo = projectVM.subProjectAllFileModels.Where(c => c.ProjectFileId == checkPrjId).FirstOrDefault();
                if (null == newPrjInfo)
                    return;
                if (projectFileManager.UnOccupyProjectFile(newPrjInfo))
                {
                    //解除占用成功,将本机文件检查并上传
                    if (selectItem.ApplcationName == EApplcationName.SU)
                    {
                        CheckLocalFileServices.Instance.ForceUpdateProjectFile(newPrjInfo);
                        CheckLocalFileServices.Instance.RemoveCheckFile(checkPrjId);
                    }
                }
                projectVM.ChangeSelectSubProject();
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("占用失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK);
            }
            finally { }
            
        }

        private void OccupyMenu_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var selectItem = menuItem.CommandParameter as ShowProjectFile;
            if (selectItem == null || string.IsNullOrEmpty(selectItem.MainFile.FileLocalPath))
                return;
            try
            {
                projectVM.ChangeSelectSubProject();
                var checkPrjId = selectItem.ProjectFileId;
                var newPrjInfo = projectVM.subProjectAllFileModels.Where(c => c.ProjectFileId == checkPrjId).FirstOrDefault();
                if (null == newPrjInfo)
                    return;
                //newPrjInfo.OccupyId = loginUser.UserLogin.Username;
                CheckLocalFileAndOpen(newPrjInfo, false);
                //newPrjInfo.OccupyId = "";
                projectFileManager.OccupyProjectFile(selectItem);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("占用失败，{0}", ex.Message), "操作提醒", MessageBoxButton.OK);
            }
            finally 
            {
                projectVM.ChangeSelectSubProject();
            }
            
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            InitUserSelectProject();
            var btn = sender as Button;
            var appName = btn.CommandParameter.ToString();
            if (string.IsNullOrEmpty(appName))
                return;
            var eAppName = EnumUtil.GetEnumItemByDescription<EApplcationName>(appName);
            string major = EnumUtil.GetEnumDescription(EMajor.Structure);
            var showName = string.Format("[{0}][{1}][{2}]", subProject.ShowName, appName, major);
            var showHis = projectFileManager.GetProjectFileHistory(pProject.PrjId, subProject.PrjId, appName, major);
            var fileHistory = new ProjectFileHistory(showHis,showName);
            fileHistory.Owner = this;
            var res = fileHistory.ShowDialog();
            if (res != true)
                return;
            var haveChange = fileHistory.GetChangedFileInfos(out List<FileHistory> changeFileInfos);
            if (!haveChange)
                return;
            //有修改历史版本,修改数据库，删除本地对应的文件，并更新显示数据
            var changeDB = projectFileManager.ChangeNewFileInfoToDB(changeFileInfos, eAppName);
            if (!changeDB)
                return;
            var changedIds = changeFileInfos.Select(c => c.MainFileId).ToList();
            foreach (var item in projectVM.subProjectAllFileModels) 
            {
                if (!changedIds.Contains(item.MainFileId))
                    continue;
                CheckLocalFileServices.Instance.RemoveCheckFile(item.MainFileId);
                foreach (var file in item.FileInfos) 
                {
                    //删除文件
                    try
                    {
                        if (File.Exists(file.FileLocalPath))
                            File.Delete(file.FileLocalPath);
                    }
                    catch { }
                }
            }
            projectVM.ChangeSelectSubProject();
        }
        private T GetParentObject<T>(DependencyObject obj) where T : FrameworkElement
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);

            while (parent != null)
            {
                if (parent is T)
                {
                    return (T)parent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
    public enum OperateType
    {
        Close=-1,
        OpenFile,
    }
}
