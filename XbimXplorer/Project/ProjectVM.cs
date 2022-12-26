using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using THBimEngine.Application;
using THBimEngine.DBOperation;
using THBimEngine.Domain;

namespace XbimXplorer.Project
{
    class ProjectVM : NotifyPropertyChangedBase
    {
        public List<string> MajorNames = new List<string>();
        public List<string> TypeNames = new List<string>();
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

        public List<ShowProjectFile> subProjectAllFileModels;
        ObservableCollection<ShowProjectFile> _cadModels { get; set; }
        public ObservableCollection<ShowProjectFile> CadModels
        {
            get { return _cadModels; }
            set
            {
                _cadModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ShowProjectFile> _suModels { get; set; }
        public ObservableCollection<ShowProjectFile> SuModels
        {
            get { return _suModels; }
            set
            {
                _suModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ShowProjectFile> _ifcModels { get; set; }
        public ObservableCollection<ShowProjectFile> IfcModels
        {
            get { return _ifcModels; }
            set
            {
                _ifcModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ShowProjectFile> _ydbModels { get; set; }
        public ObservableCollection<ShowProjectFile> YDBModels
        {
            get { return _ydbModels; }
            set
            {
                _ydbModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ShowFileLink> fileLinks { get; set; }
        public ObservableCollection<ShowFileLink> FileLinks
        {
            get { return fileLinks; }
            set
            {
                fileLinks = value;
                this.RaisePropertyChanged();
            }
        }
        private ShowProjectFile cadSelectProjectFile { get; set; }
        public ShowProjectFile CADSelectProjectFile
        {
            get { return cadSelectProjectFile; }
            set
            {
                if (null != value)
                {
                    IFCSelectProjectFile = null;
                    SUSelectProjectFile = null;
                    YDBSelectProjectFile = null;
                }
                cadSelectProjectFile = value;
                LastSelectProjectFile = value;
                this.RaisePropertyChanged();
            }
        }
        private ShowProjectFile ydbSelectProjectFile { get; set; }
        public ShowProjectFile YDBSelectProjectFile
        {
            get { return ydbSelectProjectFile; }
            set
            {
                if (null != value)
                {
                    IFCSelectProjectFile = null;
                    SUSelectProjectFile = null;
                    CADSelectProjectFile = null;
                }
                ydbSelectProjectFile = value;
                LastSelectProjectFile = value;
                this.RaisePropertyChanged();
            }
        }
        private ShowProjectFile suSelectProjectFile { get; set; }
        public ShowProjectFile SUSelectProjectFile
        {
            get { return suSelectProjectFile; }
            set
            {
                if (null != value) 
                {
                    IFCSelectProjectFile = null;
                    YDBSelectProjectFile = null;
                    CADSelectProjectFile = null;
                }
                suSelectProjectFile = value;
                LastSelectProjectFile = value;
                this.RaisePropertyChanged();
            }
        }
        private ShowProjectFile ifcSelectProjectFile { get; set; }
        public ShowProjectFile IFCSelectProjectFile
        {
            get { return ifcSelectProjectFile; }
            set
            {
                if (null != value)
                {
                    SUSelectProjectFile = null;
                    YDBSelectProjectFile = null;
                    CADSelectProjectFile = null;
                }
                ifcSelectProjectFile = value;
                LastSelectProjectFile = value;
                this.RaisePropertyChanged();
            }
        }
        private ShowProjectFile lastSelectProjectFile { get; set; }
        public ShowProjectFile LastSelectProjectFile
        {
            get { return lastSelectProjectFile; }
            set
            {
                lastSelectProjectFile = value;
                SelectProjectFileChanged();
                this.RaisePropertyChanged();
            }
        }
        private ProjectFileManager projectManager;
        public ProjectVM(List<DBProject> projects, ProjectFileManager projectFileManager)
        {
            FileLinks = new ObservableCollection<ShowFileLink>();
            AllProjects = new List<ShowProject>();
            Projects = new ObservableCollection<ShowProject>();
            projectManager = projectFileManager;
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
                        if (item.IsChild)
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
            subProjectAllFileModels = new List<ShowProjectFile>();
            YDBModels = new ObservableCollection<ShowProjectFile>();
            CadModels = new ObservableCollection<ShowProjectFile>();
            SuModels = new ObservableCollection<ShowProjectFile>();
            IfcModels = new ObservableCollection<ShowProjectFile>();
            if (null == selectProject || !selectProject.IsChild)
                return;
            CreateProjectDir(selectProject);
            var pProject = GetParentPrject(selectProject);
            var prjFiles = projectManager.GetProjectFiles(pProject, selectProject);
            foreach (var item in prjFiles)
            {
                subProjectAllFileModels.Add(item);
                if (item.SubPrjId != selectProject.PrjId)
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
            CheckDelLocalFile(pProject, selectProject);
        }
        private void CheckDelLocalFile(ShowProject pPrj,ShowProject subPrj) 
        {
            var delHis = projectManager.GetProjectDeleteFiles(pPrj, subPrj);
            var delErrorMsgs = new Dictionary<string, string>();
            if (null == delHis || delHis.Count < 1)
                return;
            foreach (var item in delHis) 
            {
                //判断作废项目是否和现有的项目名称完全相同，相同的不进行删除
                if (subProjectAllFileModels.Any(c => c.PrjId == item.PrjId
                    && c.SubPrjId == item.SubPrjId
                    && c.ApplcationName == item.ApplcationName
                    && c.Major == item.Major
                    && c.ShowFileName == item.ShowFileName))
                    continue;
                foreach (var file in item.FileInfos) 
                {
                    if (string.IsNullOrEmpty(file.FileLocalPath))
                        continue;
                    try
                    {
                        File.Delete(file.FileLocalPath);
                    }
                    catch (Exception ex) 
                    {
                        if(!delErrorMsgs.ContainsKey(file.FileLocalPath))
                            delErrorMsgs.Add(file.FileLocalPath, ex.Message);
                    }
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
        private void SelectProjectFileChanged() 
        {
            FileLinks.Clear();
            if (lastSelectProjectFile == null)
                return;
            var hisLinks = projectManager.GetMainFileLinkInfo(new List<string> { lastSelectProjectFile.ProjectFileId });
            foreach(var item in hisLinks) 
            {
                FileLinks.Add(item);
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
                        ProjectCommon.GetProjectSubDir(pPrj, child, projectManager.location, major, item, true);
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
}
