using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using THBimEngine.Application;
using THBimEngine.DBOperation;
using THBimEngine.Domain;
using THBimEngine.HttpService;
using XbimXplorer.ThBIMEngine;

namespace XbimXplorer
{
    /// <summary>
    /// ProjectManage.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectManage : Window
    {
        ProjectDBHelper projectDBHelper = new ProjectDBHelper();
        static ProjectVM projectVM =null;
        OperateType operateType;
        ProjectFileInfo selectProjectFile;
        UserInfo loginUser;
        
        public ProjectManage(UserInfo user)
        {
            InitializeComponent();
            loginUser = user;
            operateType = OperateType.Close;
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
            loaclPrjPath = projectVM.GetProjectDir(pProject);
            return pProject;
        }
        private void InitUserProjects()
        {
            if (null == projectVM) 
            {
                var userPojects = projectDBHelper.GetUserProjects(loginUser.PreSSOId);
                projectVM = new ProjectVM(userPojects);
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
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            var selectPath = new SelectUploadFile(type, projectVM.MajorNames, true);
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
                var path = projectVM.GetPrjectSubDir(projectVM.SelectProject, major, type);
                path = Path.Combine(path, oldFileName);
                if (Path.Equals(filePath, path))
                    return;
                File.Copy(filePath, path, true);
                projectVM.ChangeSelectSubProject();
                if (type == "YDB") 
                {
                    ThYDBToIfcConvertService ydbToIfc = new ThYDBToIfcConvertService();
                    ydbToIfc.Convert(path);
                }
            }
        }
        private void btnAddSUFile_Click(object sender, RoutedEventArgs e)
        {
            if (projectVM.SelectProject == null || !projectVM.SelectProject.IsChild)
                return;
            var btn = sender as Button;
            btn.IsEnabled = false;
            var selectPath = new SelectUploadFile("SU", projectVM.MajorNames, false);
            selectPath.Owner = this;
            if (selectPath.ShowDialog() == true)
            {
                var fileName = selectPath.GetSelectResult(out string major);
                if (string.IsNullOrEmpty(fileName))
                    return;
                var path =projectVM.GetPrjectSubDir(projectVM.SelectProject, major, "SU");
                if (string.IsNullOrEmpty(path))
                    return;
                var currentDir = System.Environment.CurrentDirectory;
                var templatePath = Path.Combine(currentDir, "Template\\THSKPTemplate2020.skp");
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
    }
    class ProjectVM : NotifyPropertyChangedBase
    {
        public List<string> MajorNames = new List<string>();
        public List<string> TypeNames = new List<string>();
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
            Projects = new ObservableCollection<ShowProject>();
            var allName = EnumUtil.GetEnumDescriptions<EMajor>();
            MajorNames.AddRange(allName);
            allName = EnumUtil.GetEnumDescriptions<EApplcationName>();
            TypeNames.AddRange(allName);
            foreach (var item in projects)
            {
                var prj = new ShowProject(item.Id, item.PrjNo, item.PrjName);
                Projects.Add(prj);
                foreach (var subPrj in item.SubProjects)
                {
                    var subShowPrj = new ShowProject(subPrj.SubentryId, subPrj.SubentryId, subPrj.SubEntryName, item.Id);
                    Projects.Add(subShowPrj);
                }
            }
            foreach (var item in Projects)
                item.PropertyChanged += Project_PropertyChanged;
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
            var dir = GetProjectDir(pProject);
            //dir = Path.Combine(dir, selectProject.ShowName);
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
                        GetPrjectSubDir(child, major,item);
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
        public string GetProjectDir(ShowProject project) 
        {
            var pProject = project;
            if (project.IsChild)
                pProject = GetParentPrject(project);
            var path = string.Format("D:\\THBimTempFilePath\\{0}_{1}", pProject.PrjId, pProject.ShowName);
            CheckAndAddDir(path);
            return path;
        }
        public string GetPrjectSubDir(ShowProject subProject) 
        {
            var prjPath = GetProjectDir(subProject);
            var childName = string.Format("{0}_{1}", subProject.PrjId, subProject.ShowName);
            var childDir = Path.Combine(prjPath, childName);
            CheckAndAddDir(childDir);
            return childDir;
        }
        public string GetPrjectSubDir(ShowProject subProject,string majorName)
        {
            var prjPath = GetPrjectSubDir(subProject);
            var childDir = Path.Combine(prjPath, majorName);
            CheckAndAddDir(childDir);
            return childDir;
        }
        public string GetPrjectSubDir(ShowProject subProject, string majorName,string typeName)
        {
            var prjPath = GetPrjectSubDir(subProject,majorName);
            var childDir = Path.Combine(prjPath, typeName);
            CheckAndAddDir(childDir);
            return childDir;
        }
        private void CheckAndAddDir(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
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
