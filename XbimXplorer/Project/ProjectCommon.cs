using System.Collections.Generic;
using System.IO;
using THBimEngine.DBOperation;
using THBimEngine.DBOperation.DBModels;
using THBimEngine.Domain;

namespace XbimXplorer.Project
{
    class ProjectCommon
    {
        private static string BaseDirPath = "D:\\THBimTempFilePath";
        public static string GetRootPath(string location)
        {
            var path = Path.Combine(BaseDirPath, location);
            CheckAndAddDir(path);
            return path;
        }
        public static string GetParentProjectPath(ShowProject parentProject,string location, bool haveRootDir)
        {
            var path = string.Format("{0}_{1}", parentProject.PrjId, parentProject.ShowName);
            if (haveRootDir)
            {
                path = Path.Combine(GetRootPath(location), path);
                CheckAndAddDir(path);
            }
            return path;
        }
        public static string GetSubProjectPath(ShowProject parentProject, ShowProject subProject, string location, bool haveRootDir) 
        {
            var rootPath = GetParentProjectPath(parentProject, location, haveRootDir);
            var childName = string.Format("{0}_{1}", subProject.PrjId, subProject.ShowName);
            var childDir = Path.Combine(rootPath, childName);
            if (haveRootDir)
            {
                CheckAndAddDir(childDir);
            }
            return childDir;
        }
        private static string GetProjectSubDir(ShowProject parentProject, ShowProject subProject, string location, string majorName, bool haveRootDir)
        {
            var prjPath = GetSubProjectPath(parentProject, subProject, location, haveRootDir);
            var childDir = Path.Combine(prjPath, majorName);
            if (haveRootDir)
                CheckAndAddDir(childDir);
            return childDir;
        }
        public static string GetProjectSubDir(ShowProject parentProject, ShowProject subProject, string location, string majorName, string typeName, bool haveRootDir)
        {
            var prjPath = GetProjectSubDir(parentProject, subProject, location, majorName, haveRootDir);
            var childDir = Path.Combine(prjPath, typeName);
            if (haveRootDir)
                CheckAndAddDir(childDir);
            return childDir;
        }
        public static string GetProjectParentDir(ProjectInfo projectInfo, string location, bool haveRootDir)
        {
            var path = string.Format("{0}_{1}",projectInfo.Id, projectInfo.PrjName);
            if (haveRootDir)
            {
                path = Path.Combine(GetRootPath(location), path);
                CheckAndAddDir(path);
            }
            return path;
        }
        public static string GetProjectSubDir(ProjectInfo projectInfo, string location, bool haveRootDir)
        {
            var path = GetProjectParentDir(projectInfo, location, haveRootDir);
            var childName = string.Format("{0}_{1}", projectInfo.SubentryId, projectInfo.SubEntryName);
            path = Path.Combine(path, childName);
            if (haveRootDir)
            {
                CheckAndAddDir(path);
            }

            return path;
        }
        public static string GetProjectSubDir(ProjectInfo projectInfo, string location, string majorName, string type, bool haveRootDir) 
        {
            var path = GetProjectSubDir(projectInfo, location, haveRootDir);
            path = Path.Combine(path, majorName);
            path = Path.Combine(path, type);
            if (haveRootDir)
            {
                CheckAndAddDir(path);
            }
            return path;
        }
        public static void CheckAndAddDir(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
        public static ShowFileLink DBLinkToShowLink(DBLink link)
        {
            var showLink = new ShowFileLink();
            showLink.LinkId = link.LinkId;
            showLink.MoveX = link.LinkMoveX;
            showLink.MoveY = link.LinkMoveY;
            showLink.MoveZ = link.LinkMoveZ;
            showLink.FromLinkId = link.FromLinkId;
            showLink.RotainAngle = link.LinkRotainAngle;
            showLink.ProjectFileId = link.ProjectFileId;
            showLink.LinkProjectFileId = link.LinkProjectFileId;
            showLink.State = link.LinkState;
            return showLink;
        }
        public static FileDetail DBVProjectFileToFileDetail(DBVProjectFile projectFile) 
        {
            var fileDetail = new FileDetail();
            fileDetail.FileId = projectFile.FileId;
            fileDetail.ProjectUploadId = projectFile.ProjectUploadId;
            fileDetail.ProjectFileId = projectFile.ProjectFileId;
            fileDetail.FileDownloadPath = projectFile.FileDownloadPath;
            fileDetail.Uploader = projectFile.Uploader;
            fileDetail.UploaderName = projectFile.UploaderName;
            fileDetail.FileMD5 = projectFile.FileMD5;
            fileDetail.FileRealName = projectFile.FileRealName;
            fileDetail.UploadTime = projectFile.UploadTime;
            fileDetail.IsMainFile = projectFile.IsMainFile == 1;
            fileDetail.NeedDownload = projectFile.NeedDownload == 1;
            fileDetail.CanOpen = projectFile.CanOpen == 1;
            return fileDetail;
        }
        public static ShowProjectFile DBVProjectMainFileToProjectShowFile(DBVProjectMainFile projectMainFile) 
        {
            var showPrjFile = new ShowProjectFile();
            showPrjFile.ProjectFileId = projectMainFile.ProjectFileId;
            showPrjFile.ProjectMainFileUploadId = projectMainFile.FileUploadId;
            showPrjFile.PrjId = projectMainFile.PrjId;
            showPrjFile.SubPrjName = projectMainFile.SubPrjName;
            showPrjFile.PrjName = projectMainFile.PrjName;
            showPrjFile.SubPrjId = projectMainFile.SubPrjId;
            showPrjFile.ShowFileName = projectMainFile.FileName;
            showPrjFile.Major = EnumUtil.GetEnumItemByDescription<EMajor>(projectMainFile.MajorName);
            showPrjFile.ApplcationName = EnumUtil.GetEnumItemByDescription<EApplcationName>(projectMainFile.ApplicationName);
            if (showPrjFile.ApplcationName == EApplcationName.CAD)
            {
                showPrjFile.ShowSourceName = "主体";
            }
            else
            {
                showPrjFile.ShowSourceName = showPrjFile.ApplcationName.ToString();
            }
            showPrjFile.OwnerId = projectMainFile.CreaterId;
            showPrjFile.OwnerName = projectMainFile.CreaterName;
            return showPrjFile;
        }
        public static ShowProjectFile DBVProjectMainFileToProjectShowFile(DBVProjectMainDelFile projectDelFile)
        {
            var showPrjFile = new ShowProjectFile();
            showPrjFile.ProjectFileId = projectDelFile.ProjectFileId;
            showPrjFile.PrjId = projectDelFile.PrjId;
            showPrjFile.SubPrjName = projectDelFile.SubPrjName;
            showPrjFile.PrjName = projectDelFile.PrjName;
            showPrjFile.SubPrjId = projectDelFile.SubPrjId;
            showPrjFile.ShowFileName = projectDelFile.FileName;
            showPrjFile.Major = EnumUtil.GetEnumItemByDescription<EMajor>(projectDelFile.MajorName);
            showPrjFile.ApplcationName = EnumUtil.GetEnumItemByDescription<EApplcationName>(projectDelFile.ApplicationName);
            return showPrjFile;
        }
        public static ShortProjectFile ShortProjectFileToShortData(ShowProjectFile showProjectFile)
        {
            var shortData = new ShortProjectFile();
            shortData.ApplcationName = showProjectFile.ApplcationName;
            shortData.MainFileId = showProjectFile.MainFileId;
            shortData.ProjectFileId = showProjectFile.ProjectFileId;
            shortData.ProjectMainFileUploadId = showProjectFile.ProjectMainFileUploadId;
            shortData.SubPrjId = showProjectFile.SubPrjId;
            shortData.PrjId = showProjectFile.PrjId;
            shortData.Major = showProjectFile.Major;
            return shortData;
        }
        public static FileHistory DBProjectFileToFileHistory(DBProjectFile projectFile)
        {
            var fileHistory = new FileHistory();
            fileHistory.MainFileId = projectFile.ProjectFileId;
            fileHistory.MainFileName = projectFile.FileName;
            fileHistory.State = projectFile.IsDel == 0 ? "" : "已作废";
            fileHistory.FileHistoryDetails = new List<FileHistoryDetail>();
            return fileHistory;
        }
        public static FileHistoryDetail DBVFileInfoToFileDetail(DBVAllProjectFile projectFile) 
        {
            var fileHistoryDetail = new FileHistoryDetail();
            fileHistoryDetail.ProjectFileId = projectFile.ProjectFileId;
            fileHistoryDetail.ShowFileName = projectFile.FileRealName;
            fileHistoryDetail.UploaderName = projectFile.UploaderName;
            fileHistoryDetail.ProjectFileUplaodId = projectFile.ProjectUploadId;
            fileHistoryDetail.FileUploadTime = projectFile.UploadTime;
            fileHistoryDetail.ProjectFileUploadVersionId = projectFile.VersionId;
            return fileHistoryDetail;
        }
    }
}
