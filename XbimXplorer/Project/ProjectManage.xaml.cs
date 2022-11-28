﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using THBimEngine.Application;
using THBimEngine.DBOperation;
using THBimEngine.Domain;
using THBimEngine.HttpService;
using XbimXplorer.Project;
using XbimXplorer.ThBIMEngine;

namespace XbimXplorer
{
    /// <summary>
    /// ProjectManage.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectManage : Window
    {
        ProjectDBHelper projectDBHelper;
        static ProjectVM projectVM =null;
        OperateType operateType;
        ProjectFileInfo selectProjectFile;
        UserInfo loginUser;
        ShowProject parentProject = null;
        ShowProject subProject = null;
        public ProjectManage(UserInfo user)
        {
            InitializeComponent();
            loginUser = user;
            operateType = OperateType.Close;
            projectDBHelper = new ProjectDBHelper(user.LocationSQLIp);
            InitUserProjects();
        }
        public OperateType GetOperateType() 
        {
            return operateType;
        }
        public ProjectFileInfo GetOpenModel() 
        {
            return selectProjectFile;
        }
        private void InitSelectProject() 
        {
            parentProject = null;
            subProject = null;
            if (null == projectVM.SelectProject || !projectVM.SelectProject.IsChild)
                return;
            subProject = projectVM.SelectProject;
            parentProject = projectVM.GetParentPrject(subProject);
        }
        public ShowProject GetSelectPrjSubPrj(out ShowProject subProject, out List<ShowProject> allSubPrjs, out string loaclPrjPath) 
        {
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
            loaclPrjPath = ProjectCommon.GetProjectDir(pProject);
            return pProject;
        }
        private void InitUserProjects()
        {
            if (null == projectVM) 
            {
                var userPojects = projectDBHelper.GetUserProjects(loginUser.PreSSOId);
                projectVM = new ProjectVM(userPojects);
                projectVM.SelectMajorName = loginUser.Majors.FirstOrDefault();
            }
            projectVM.MajorNames.Clear();
            foreach (var item in loginUser.Majors)
            {
                projectVM.MajorNames.Add(item);
                majorControl.Items.Add(item);
            }
            this.DataContext = projectVM;
        }

        private void btnUploadIFC_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            SelectAndUploadFile("IFC");
            btn.IsEnabled = true;
        }
        private void SelectAndUploadFile(string type)
        {
            InitSelectProject();
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            var selectPath = new SelectUploadFile(type, new List<string> { projectVM.SelectMajorName}, true);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() == true)
            {
                var filePath = selectPath.GetSelectResult(out string major);
                if (string.IsNullOrEmpty(filePath))
                    return;
                var oldFileName = Path.GetFileName(filePath);
                var extName = Path.GetExtension(filePath);
                var newFileName = System.Guid.NewGuid().ToString();
                var osskey = string.Format("{0}{1}", newFileName, extName);
                //S3HttpFile s3HttpFile = new S3HttpFile();
                //s3HttpFile.UploadFile(filePath, osskey);
                var path = ProjectCommon.GetPrjectSubDir(parentProject,subProject, major, type);
                path = Path.Combine(path, oldFileName);
                if (Path.Equals(filePath, path))
                    return;
                File.Copy(filePath, path, true);
                if (type == "YDB") 
                {
                    ThYDBToIfcConvertService ydbToIfc = new ThYDBToIfcConvertService();
                    ydbToIfc.Convert(path);
                }
                projectVM.ChangeSelectSubProject();
            }
        }
        private void btnAddSUFile_Click(object sender, RoutedEventArgs e)
        {
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            var btn = sender as Button;
            btn.IsEnabled = false;
            var selectPath = new SelectUploadFile("SU", new List<string> { projectVM.SelectMajorName }, false);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() == true)
            {
                var fileName = selectPath.GetSelectResult(out string major);
                if (string.IsNullOrEmpty(fileName))
                    return;
                var path = ProjectCommon.GetPrjectSubDir(parentProject,subProject, major, "SU");
                if (string.IsNullOrEmpty(path))
                    return;
                var currentDir = System.Environment.CurrentDirectory;
                var templatePath = Path.Combine(currentDir, "Template\\THSKPTemplate_S_2020.skp");
                path = Path.Combine(path, fileName + ".skp");
                File.Copy(templatePath, path, true);
                //OpenFile(path);
                projectVM.ChangeSelectSubProject();
            }
            btn.IsEnabled = true;
        }
        private void btnUploadYDB_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            btn.IsEnabled = false;
            SelectAndUploadFile("YDB");
            btn.IsEnabled = true;
        }

        private void Row_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender == null)
                return;
            var dGridRow = sender as DataGridRow;
            if (dGridRow == null)
                return;
            var fileInfo = dGridRow.DataContext as ProjectFileInfo;
            if (fileInfo == null)
                return;
            if (fileInfo.ApplcationName == EApplcationName.SU)
            {
                OpenFile(fileInfo.LoaclPath);
            }
            else if(fileInfo.CanLink)
            {
                selectProjectFile = fileInfo;
                operateType = OperateType.OpenFile;
                this.DialogResult = true;
                this.Close();
            }
            
        }
        private void OpenFile(string filePath) 
        {
            if (string.IsNullOrEmpty(filePath))
                return;
            if (!File.Exists(filePath))
                return;
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
            var selectItem = menuItem.CommandParameter as ProjectFileInfo;
            if (null == selectItem)
                return;
            var strMsg = string.Format("确定要作废 {0} 下的 {1} 专业的 {2} 文件吗，该过程是不可逆的，是否继续操作？", selectItem.SubPrjName, selectItem.MajorName, selectItem.ShowFileName);
            var res = MessageBox.Show(strMsg, "操作提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return;
            if (!string.IsNullOrEmpty(selectItem.LoaclPath) && File.Exists(selectItem.LoaclPath))
            {
                File.Delete(selectItem.LoaclPath);
            }
            if (!string.IsNullOrEmpty(selectItem.LinkFilePath) && File.Exists(selectItem.LinkFilePath))
            {
                File.Delete(selectItem.LinkFilePath);
            }
            if (!string.IsNullOrEmpty(selectItem.ExternalLinkPath) && File.Exists(selectItem.ExternalLinkPath))
            {
                File.Delete(selectItem.ExternalLinkPath);
            }
            projectVM.ChangeSelectSubProject();
        }
        private void UpdateFile_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            var selectItem = menuItem.CommandParameter as ProjectFileInfo;
            if (selectItem == null || string.IsNullOrEmpty(selectItem.LoaclPath))
                return;
            var path = selectItem.LoaclPath;
            var type = selectItem.ApplcationName.ToString();
            var selectPath = new SelectUploadFile(type, new List<string> { selectItem.MajorName}, true);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() == true)
            {
                var filePath = selectPath.GetSelectResult(out string major);
                if (string.IsNullOrEmpty(filePath))
                    return;
                File.Copy(filePath, path, true);
                if (type == "YDB")
                {
                    ThYDBToIfcConvertService ydbToIfc = new ThYDBToIfcConvertService();
                    ydbToIfc.Convert(path);
                }
                projectVM.ChangeSelectSubProject();
            }
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radio = sender as RadioButton;
            projectVM.SelectMajorName = radio.Content.ToString();
        }
    }
    class ProjectVM : NotifyPropertyChangedBase
    {
        public List<string> MajorNames = new List<string>();
        public List<string> TypeNames = new List<string>();
        private string selectMajorName { get; set; }
        public string SelectMajorName
        {
            get { return selectMajorName; }
            set
            {
                selectMajorName = value;
                if (!string.IsNullOrEmpty(selectMajorName))
                    SelectMajor = EnumUtil.GetEnumItemByDescription<EMajor>(selectMajorName);
                this.RaisePropertyChanged();
            }
        }
        private EMajor selectMajor { get; set; }
        public EMajor SelectMajor
        {
            get
            {
                return selectMajor;
            }
            set
            {
                selectMajor = value;
                FilterByMajor();
                this.RaisePropertyChanged();
            }
        }
        public List<ShowProject> AllProjects { get; set; }
        ObservableCollection<ShowProject> _projectModels { get; set; }
        public ObservableCollection<ShowProject> Projects
        {
            get { return _projectModels; }
            set
            {
                _projectModels = value;
                this.RaisePropertyChanged();
            }
        }
        private ShowProject selectProject { get; set; }
        public ShowProject SelectProject
        {
            get
            {
                return selectProject;
            }
            set
            {
                selectProject = value;
                ChangeSelectSubProject();
                this.RaisePropertyChanged();
            }
        }

        private List<ProjectFileInfo> subProjectAllFileModels;
        ObservableCollection<ProjectFileInfo> _cadModels { get; set; }
        public ObservableCollection<ProjectFileInfo> CadModels
        {
            get { return _cadModels; }
            set
            {
                _cadModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ProjectFileInfo> _suModels { get; set; }
        public ObservableCollection<ProjectFileInfo> SuModels
        {
            get { return _suModels; }
            set
            {
                _suModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ProjectFileInfo> _ifcModels { get; set; }
        public ObservableCollection<ProjectFileInfo> IfcModels
        {
            get { return _ifcModels; }
            set
            {
                _ifcModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ProjectFileInfo> _ydbModels { get; set; }
        public ObservableCollection<ProjectFileInfo> YDBModels
        {
            get { return _ydbModels; }
            set
            {
                _ydbModels = value;
                this.RaisePropertyChanged();
            }
        }

        public ProjectVM(List<DBProject> projects)
        {
            AllProjects = new List<ShowProject>();
            Projects = new ObservableCollection<ShowProject>();
            var allName = EnumUtil.GetEnumDescriptions<EMajor>();
            MajorNames.AddRange(allName);
            allName = EnumUtil.GetEnumDescriptions<EApplcationName>();
            TypeNames.AddRange(allName);
            foreach (var item in projects)
            {
                var prj = new ShowProject(item.Id, item.PrjNo, item.PrjName);
                AllProjects.Add(prj);
                foreach (var subPrj in item.SubProjects)
                {
                    var subShowPrj = new ShowProject(subPrj.SubentryId, subPrj.SubentryId, subPrj.SubEntryName, item.Id);
                    AllProjects.Add(subShowPrj);
                }
            }
            FilterProject("");
        }
        public void FilterProject(string searchText) 
        {
            foreach (var item in Projects)
                item.PropertyChanged -= Project_PropertyChanged;
            Projects.Clear();
            if (!string.IsNullOrEmpty(searchText))
            {
                var showPPrjIds = new List<string>();
                foreach (var item in AllProjects)
                {
                    if (item.PrjNum.Contains(searchText) || item.ShowName.Contains(searchText))
                    {
                        string addId = item.PrjId;
                        if (item.IsChild)
                            addId = item.ParentId;
                        if (showPPrjIds.Contains(addId))
                            continue;
                        showPPrjIds.Add(addId);
                    }
                }
                foreach (var pId in showPPrjIds) 
                {
                    var prjs = AllProjects.Where(c => c.PrjId == pId || c.ParentId == pId).ToList();
                    var pPrj = prjs.Where(c => c.PrjId == pId).First();
                    Projects.Add(pPrj);
                    foreach (var item in prjs) 
                    {
                        if(item.IsChild)
                            Projects.Add(item);
                    }
                }
            }
            else 
            {
                foreach (var item in AllProjects)
                {
                    Projects.Add(item);
                }
            }
            foreach (var item in Projects)
                item.PropertyChanged += Project_PropertyChanged;
            foreach (var item in Projects) 
            {
                if (item.IsChild)
                    continue;
                item.IsExpand = false;
            }
        }
        public void ChangeSelectSubProject() 
        {
            subProjectAllFileModels = new List<ProjectFileInfo>();
            YDBModels = new ObservableCollection<ProjectFileInfo>();
            CadModels = new ObservableCollection<ProjectFileInfo>();
            SuModels = new ObservableCollection<ProjectFileInfo>();
            IfcModels = new ObservableCollection<ProjectFileInfo>();
            if (null == selectProject || !selectProject.IsChild)
                return;
            CreateProjectDir(selectProject);
            var pProject = GetParentPrject(selectProject);
            var dir = ProjectCommon.GetProjectDir(pProject);
            var fileProject = new FileProject(dir);
            var mainFileInfos = fileProject.GetProjectFiles();
            foreach (var item in mainFileInfos) 
            {
                subProjectAllFileModels.Add(item);
                if (item.SubPrjId != selectProject.PrjId)
                    continue;
                if (item.ApplcationName == EApplcationName.CAD)
                    CadModels.Add(item);
                switch (item.ApplcationName) 
                {
                    case EApplcationName.CAD:
                        CadModels.Add(item);
                        break;
                    case EApplcationName.IFC:
                        IfcModels.Add(item);
                        break;
                    case EApplcationName.SU:
                        SuModels.Add(item);
                        break;
                    case EApplcationName.YDB:
                        YDBModels.Add(item);
                        break;
                }
            }
        }
        private void Project_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var changePrj = sender as ShowProject;
            if (changePrj.IsChild)
                return;
            if (e.PropertyName == "IsExpand")
            {
                var res = changePrj.IsExpand ? Visibility.Visible : Visibility.Collapsed;
                foreach (var item in Projects)
                {
                    if (!item.IsChild)
                        continue;
                    if (item.ParentId == changePrj.PrjId)
                        item.RowVisibility = res;
                }
            }
        }

        private void CreateProjectDir(ShowProject showProject)
        {
            var onePrj = GetOneProject(showProject);
            var pPrj = onePrj[0];
            //GetProjectDir(pPrj);
            for (int i = 1; i < onePrj.Count; i++)
            {
                var child = onePrj[i];
                //GetPrjectSubDir(child);
                foreach (var major in MajorNames)
                {
                    //var majorDir = GetPrjectSubDir(child, major);
                    foreach (var item in TypeNames)
                    {
                        ProjectCommon.GetPrjectSubDir(pPrj,child, major,item);
                    }
                }
            }
        }
        
        public List<ShowProject> GetOneProject(ShowProject showProject)
        {
            var prjects = new List<ShowProject>();
            var pPrj = GetParentPrject(showProject);
            var childs = GetAllChildProject(pPrj);
            prjects.Add(pPrj);
            prjects.AddRange(childs);
            return prjects;
        }
        public ShowProject GetParentPrject(ShowProject prj)
        {
            if (!prj.IsChild)
                return prj;
            foreach (var item in Projects)
            {
                if (item.IsChild || item.PrjId != prj.ParentId)
                    continue;
                return item;
            }
            return null;
        }
        public List<ShowProject> GetAllChildProject(ShowProject pPrj)
        {
            var prjects = new List<ShowProject>();
            foreach (var item in Projects)
            {
                if (item.ParentId != pPrj.PrjId)
                    continue;
                prjects.Add(item);
            }
            return prjects;
        }
        private void FilterByMajor()
        {
            if (null == subProjectAllFileModels)
                return;
            YDBModels = new ObservableCollection<ProjectFileInfo>();
            CadModels = new ObservableCollection<ProjectFileInfo>();
            SuModels = new ObservableCollection<ProjectFileInfo>();
            IfcModels = new ObservableCollection<ProjectFileInfo>();
            foreach (var item in subProjectAllFileModels)
            {
                if (item.SubPrjId != selectProject.PrjId)
                    continue;
                if (item.Major != selectMajor)
                    continue;
                switch (item.ApplcationName)
                {
                    case EApplcationName.CAD:
                        CadModels.Add(item);
                        break;
                    case EApplcationName.IFC:
                        IfcModels.Add(item);
                        break;
                    case EApplcationName.SU:
                        SuModels.Add(item);
                        break;
                    case EApplcationName.YDB:
                        YDBModels.Add(item);
                        break;
                }
            }
        }
    }
    public class ShowProject : NotifyPropertyChangedBase
    {
        public string PrjId { get; set; }
        public string ParentId { get; }
        public string PrjNum { get; set; }
        public string ShowName { get; set; }
        public bool IsChild
        {
            get { return !string.IsNullOrEmpty(ParentId); }
        }
        private bool isExpand { get; set; }
        public bool IsExpand
        {
            get { return isExpand; }
            set
            {
                isExpand = value;
                this.RaisePropertyChanged();
            }
        }
        private Visibility rowVisibility { get; set; }
        public Visibility RowVisibility
        {
            get { return rowVisibility; }
            set
            {
                rowVisibility = value;
                this.RaisePropertyChanged();
            }
        }
        public ShowProject(string prjId, string prjNum, string prjName) : this(prjId, prjNum, prjName, "")
        {
        }
        public ShowProject(string prjId, string prjNum, string prjName, string parentId)
        {
            PrjId = prjId;
            PrjNum = prjNum;
            ShowName = prjName;
            ParentId = parentId;
            IsExpand = true;
        }
    }

    public enum OperateType
    {
        Close=-1,
        OpenFile,
    }
}
