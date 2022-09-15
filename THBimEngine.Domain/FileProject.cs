using System.Collections.Generic;
using System.IO;

namespace THBimEngine.Domain
{
    public abstract class BimFileBase 
    {
        public string FilePath { get; protected set; }
        public string ShowName { get; set; }
    }
    public class FileProject : BimFileBase
    {
        public List<FileBuilding> ProjectBuilds { get; }
        public FileProject(string prjFilePath) 
        {
            ProjectBuilds = new List<FileBuilding>();
            this.FilePath = prjFilePath;
            this.ShowName = Path.GetFileNameWithoutExtension(this.FilePath);
            DirectoryInfo directory = new DirectoryInfo(this.FilePath);
            DirectoryInfo[] dirs = directory.GetDirectories();
            foreach (DirectoryInfo dir in dirs) 
            {
                var buildind = new FileBuilding(dir.FullName);
                if (null != buildind)
                {
                    ProjectBuilds.Add(buildind);
                }
            }
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
            foreach (var dir in dirs) 
            {
                var catagory = new FileCatagory(dir.FullName);
                if (null != catagory)
                    FileCatagories.Add(catagory);
            }
        }
    }
    public class FileCatagory: BimFileBase
    {
        public List<ModelFile> ModelFiles { get; }
        public FileCatagory(string modelPath) 
        {
            this.FilePath = modelPath;
            this.ShowName = Path.GetFileNameWithoutExtension(this.FilePath);
            ModelFiles = new List<ModelFile>();
            DirectoryInfo directory = new DirectoryInfo(this.FilePath);
            DirectoryInfo[] dirs = directory.GetDirectories();
            foreach (var dir in dirs)
            {
                var files = dir.GetFiles();
                var strName = dir.Name.ToLower();
                if (strName.Contains("cad"))
                {
                    foreach (var file in files) 
                    {
                        if (file.Extension.ToLower() == ".cadmidfile")
                        {
                            var cadModelFile = new ModelFile(file.FullName, "主体");
                            cadModelFile.MidFilePath = file.FullName;
                            ModelFiles.Add(cadModelFile);
                        }
                    }
                }
                else if (strName.Contains("ifc"))
                {
                    foreach (var file in files)
                    {
                        if (file.Extension.ToLower() == ".ifc")
                        {
                            var ifcModelFile = new ModelFile(file.FullName, "IFC");
                            ifcModelFile.MidFilePath = file.FullName;
                            ModelFiles.Add(ifcModelFile);
                        }
                    }
                }
                else if (strName.Contains("su")) 
                {
                    foreach (var file in files)
                    {
                        if (file.Extension.ToLower() == ".thbim")
                        {
                            var suModelFile = new ModelFile(file.FullName, "SU");
                            suModelFile.MidFilePath = file.FullName;
                            ModelFiles.Add(suModelFile);
                        }
                    }
                }
            }
        }
    }
    public class ModelFile : BimFileBase
    {
        public string SystemType { get; }
        public string MidFilePath { get; set; }
        public ModelFile(string path,string systemType) 
        {
            this.FilePath = path;
            this.ShowName = Path.GetFileNameWithoutExtension(this.FilePath);
            SystemType = systemType;
        }
    }
}
