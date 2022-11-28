using System.IO;
using THBimEngine.DBOperation;

namespace XbimXplorer.Project
{
    class ProjectCommon
    {
        public static string GetProjectDir(ShowProject pProject,bool haveRootPath = true)
        {
            var path = string.Format("{0}_{1}", pProject.PrjId, pProject.ShowName);
            if (haveRootPath) 
            {
                path = Path.Combine("D:\\THBimTempFilePath", path);
                CheckAndAddDir(path);
            }
            return path;
        }
        public static string GetPrjectSubDir(ShowProject pProject, ShowProject subProject, bool haveRootPath = true)
        {
            var prjPath = GetProjectDir(pProject, haveRootPath);
            var childName = string.Format("{0}_{1}", subProject.PrjId, subProject.ShowName);
            var childDir = Path.Combine(prjPath, childName);
            if(haveRootPath)
                CheckAndAddDir(childDir);
            return childDir;
        }
        public static string GetPrjectSubDir(ShowProject pProject, ShowProject subProject,string majorName, bool haveRootPath = true)
        {
            var prjPath = GetPrjectSubDir(pProject,subProject, haveRootPath);
            var childDir = Path.Combine(prjPath, majorName);
            if (haveRootPath)
                CheckAndAddDir(childDir);
            return childDir;
        }
        public static string GetPrjectSubDir(ShowProject pProject, ShowProject subProject, string majorName, string typeName, bool haveRootPath = true)
        {
            var prjPath = GetPrjectSubDir(pProject, subProject, majorName, haveRootPath);
            var childDir = Path.Combine(prjPath, typeName);
            if (haveRootPath)
                CheckAndAddDir(childDir);
            return childDir;
        }


        public static string GetProjectDir(DBSubProject pProject, bool haveRootPath = true)
        {
            var path = string.Format("{0}_{1}", pProject.Id, pProject.PrjName);
            if (haveRootPath)
            {
                path = Path.Combine("D:\\THBimTempFilePath", path);
                CheckAndAddDir(path);
            }
            return path;
        }
        public static string GetPrjectSubDir(DBSubProject project, bool haveRootPath = true)
        {
            var prjPath = GetProjectDir(project, haveRootPath);
            var childName = string.Format("{0}_{1}", project.SubentryId, project.SubEntryName);
            var childDir = Path.Combine(prjPath, childName);
            if (haveRootPath)
                CheckAndAddDir(childDir);
            return childDir;
        }
        public static string GetPrjectSubDir(DBSubProject project, string majorName, bool haveRootPath = true)
        {
            var prjPath = GetPrjectSubDir(project, haveRootPath);
            var childDir = Path.Combine(prjPath, majorName);
            if (haveRootPath)
                CheckAndAddDir(childDir);
            return childDir;
        }
        public static string GetPrjectSubDir(DBSubProject project, string majorName, string typeName, bool haveRootPath = true)
        {
            var prjPath = GetPrjectSubDir(project, majorName, haveRootPath);
            var childDir = Path.Combine(prjPath, typeName);
            if (haveRootPath)
                CheckAndAddDir(childDir);
            return childDir;
        }

        public static void CheckAndAddDir(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
