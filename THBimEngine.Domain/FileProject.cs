using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace THBimEngine.Domain
{
    public abstract class BimFileBase 
    {
        public string FilePath { get; protected set; }
        public string ShowName { get; set; }
    }
    public class FileProject : BimFileBase
    {
        private string prjId;
        public FileProject(string prjFilePath) 
        {
            this.FilePath = prjFilePath;
            var dirName = Path.GetFileNameWithoutExtension(this.FilePath);
            var spliteIndex = dirName.IndexOf("_");
            ShowName = dirName;
            if (spliteIndex > 0)
            {
                prjId = dirName.Substring(0, spliteIndex);
                ShowName = dirName.Substring(spliteIndex + 1);
            }
        }
        public List<FileBuilding> GetDirFileBuilding() 
        {
            var projectBuilds = new List<FileBuilding>();
            DirectoryInfo directory = new DirectoryInfo(this.FilePath);
            DirectoryInfo[] dirs = directory.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                var buildind = new FileBuilding(dir.FullName);
                if (null != buildind)
                {
                    projectBuilds.Add(buildind);
                }
            }
            return projectBuilds;
        }
        public List<ProjectFileInfo> GetProjectFiles(bool mainFileDataWithLinkFile = true) 
        {
            var projectBuilds = GetDirFileBuilding();
            var linkFileInfos = new List<ProjectFileInfo>();
            var mainFileInfos = new List<ProjectFileInfo>();
            var linkModelFiles = new List<ProjectFileInfo>();
            foreach (var building in projectBuilds)
            {
                foreach (var catagory in building.FileCatagories)
                {
                    foreach (var model in catagory.ModelFiles)
                    {
                        ProjectFileInfo showFile = new ProjectFileInfo();
                        showFile.PrjId = prjId;
                        showFile.SubPrjId = catagory.SubPrjId;
                        showFile.Major = catagory.Major;
                        showFile.SubPrjName = catagory.SubName;
                        showFile.ApplcationName = model.SourceType;
                        showFile.ShowSourceName = model.ShowTypeName;
                        showFile.ShowFileName = model.ShowName;
                        showFile.LoaclPath = model.FilePath;
                        FileInfo fileInfo = new FileInfo(model.FilePath);
                        showFile.LastUpdataTime = fileInfo.LastWriteTime;
                        FileSecurity fileSecurity = fileInfo.GetAccessControl();
                        if (null != fileSecurity)
                        {
                            var identityReference = fileSecurity.GetOwner(typeof(NTAccount));
                            if (null != identityReference)
                                showFile.OwnerName = identityReference.Value;
                        }
                        var config = ApplicationDefaultConfig.DefaultConfig.Where(c => c.Source == model.SourceType).FirstOrDefault();
                        if (model.SourceType == EApplcationName.IFC)
                        {
                            showFile.LinkFilePath = model.FilePath;
                            mainFileInfos.Add(showFile);
                        }
                        else
                        {
                            if (config.FileExt.Contains(model.FileExt))
                            {
                                mainFileInfos.Add(showFile);
                            }
                            else if(config.LinkFileExt.Contains(model.FileExt))
                            {
                                linkFileInfos.Add(showFile);
                            }
                            else if(config.LinkModelFileExt == model.FileExt) 
                            {
                                linkModelFiles.Add(showFile);
                            }
                        }
                    }
                }
            }
            foreach (var item in mainFileInfos)
            {
                if (!string.IsNullOrEmpty(item.LinkFilePath))
                    continue;
                var linkModel = linkFileInfos.Where(c => c.SubPrjId == item.SubPrjId && c.Major == item.Major && c.ShowFileName == item.ShowFileName).FirstOrDefault();
                if (linkModel != null)
                {
                    item.LinkFilePath = linkModel.LoaclPath;
                    if (mainFileDataWithLinkFile) 
                    {
                        FileInfo fileInfo = new FileInfo(linkModel.LoaclPath);
                        item.LastUpdataTime = fileInfo.LastWriteTime;
                    }
                }
                var linkFile = linkModelFiles.Where(c => c.SubPrjId == item.SubPrjId && c.Major == item.Major && c.ShowFileName == item.ShowFileName).FirstOrDefault();
                if (null != linkFile)
                    item.ExternalLinkPath = linkFile.LoaclPath;
                else 
                {
                    var filePath = item.LoaclPath;
                    var path = Path.GetDirectoryName(filePath);
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    item.ExternalLinkPath = Path.Combine(path, string.Format("{0}.{1}", fileName, "thlink"));
                }
            }
            return mainFileInfos;
        }
    }
    public class FileBuilding:BimFileBase
    {
        public List<FileCatagory> FileCatagories { get; }
        public FileBuilding(string buildingPath) 
        {
            this.FilePath = buildingPath;
            this.ShowName = Path.GetFileNameWithoutExtension(this.FilePath);
            FileCatagories = new List<FileCatagory>();
            DirectoryInfo directory = new DirectoryInfo(this.FilePath);
            DirectoryInfo[] dirs = directory.GetDirectories();

            var spliteIndex = ShowName.IndexOf("_");
            var subPrjId = "";
            var sbuPrjName = "";
            if (spliteIndex > 0)
            {
                subPrjId = ShowName.Substring(0, spliteIndex);
                sbuPrjName = ShowName.Substring(spliteIndex + 1);
            }
            var majorConfig = ApplicationDefaultConfig.GetMajorConfig();
            foreach (var dir in dirs) 
            {
                EMajor? major = null;
                foreach (var item in majorConfig) 
                {
                    if (dir.Name.Contains(item.Value))
                    {
                        major = item.Key;
                        break;
                    }
                }
                if (!major.HasValue)
                    continue;
                var catagory = new FileCatagory(dir.FullName, major.Value, subPrjId, sbuPrjName);
                if (null != catagory)
                    FileCatagories.Add(catagory);
            }
        }
    }
    public class FileCatagory: BimFileBase
    {
        public string SubPrjId { get; }
        public string SubName { get; }
        public EMajor Major { get; }
        public List<ModelFile> ModelFiles { get; }
        public FileCatagory(string modelPath, EMajor major,string subPrjId,string subName) 
        {
            this.FilePath = modelPath;
            Major = major;
            SubName = subName;
            this.ShowName = Path.GetFileNameWithoutExtension(this.FilePath);
            SubPrjId = subPrjId;
            ModelFiles = new List<ModelFile>();
            CalcFileDir();
        }
        private void CalcFileDir() 
        {
            DirectoryInfo directory = new DirectoryInfo(this.FilePath);
            DirectoryInfo[] dirs = directory.GetDirectories();
            foreach (var dir in dirs)
            {
                var files = dir.GetFiles();
                var strName = dir.Name.ToUpper();
                SourceConfig sourceProject = null;
                foreach (var item in ApplicationDefaultConfig.DefaultConfig) 
                {
                    if (strName.Contains(item.DirNameContain))
                    {
                        sourceProject = item;
                        break;
                    }    
                }
                if (sourceProject == null)
                    continue;
                foreach (var file in files)
                {
                    var ext = file.Extension.ToLower();
                    if (null != sourceProject.FileExt && sourceProject.FileExt.Contains(ext))
                    {
                        var modelFile = new ModelFile(file.FullName, sourceProject.ShowName, sourceProject.Source);
                        ModelFiles.Add(modelFile);
                    }
                    else if (null != sourceProject.LinkFileExt && sourceProject.LinkFileExt.Contains(ext))
                    {
                        var modelFile = new ModelFile(file.FullName, sourceProject.ShowName, sourceProject.Source);
                        ModelFiles.Add(modelFile);
                    }
                    else if (!string.IsNullOrEmpty(sourceProject.LinkModelFileExt) && sourceProject.LinkModelFileExt == ext) 
                    {
                        var modelFile = new ModelFile(file.FullName, sourceProject.ShowName, sourceProject.Source);
                        ModelFiles.Add(modelFile);
                    }
                }
            }
        }
    }
    public class ModelFile : BimFileBase
    {
        public string ShowTypeName { get; }
        public EApplcationName SourceType { get; }
        public string FileExt { get; }
        public ModelFile(string path,string systemType, EApplcationName source) 
        {
            this.FilePath = path;
            this.ShowName = Path.GetFileNameWithoutExtension(this.FilePath);
            FileExt = Path.GetExtension(this.FilePath).ToLower();
            ShowTypeName = systemType;
            SourceType = source;
        }
    }

    public class ProjectFileInfo
    {
        public string PrjId { get; set; }
        public string SubPrjId { get; set; }
        public string SubPrjName { get; set; }
        public string FileId { get; set; }
        public string LoaclPath { get; set; }
        public string ExternalLinkPath { get; set; }
        public string ShowFileName { get; set; }
        public EMajor Major { get; set; }
        public string MajorName 
        {
            get 
            {
                return EnumUtil.GetEnumDescription(Major);
            }
        }
        public EApplcationName ApplcationName { get; set; }
        public string ShowSourceName { get; set; }
        public DateTime LastUpdataTime { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OccupyId { get; set; }
        public string OccupyName { get; set; }
        public bool CanLink
        {
            get { return !string.IsNullOrEmpty(LinkFilePath); }
        }
        public string LinkFilePath { get; set; }
    }

    
}
