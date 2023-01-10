using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using THBimEngine.Common;
using THBimEngine.Domain;
using THBimEngine.HttpService;
using XbimXplorer.Project;

namespace XbimXplorer
{
    class CheckLocalFileServices
    {
        Timer checkTimer;
        public static readonly CheckLocalFileServices Instance = new CheckLocalFileServices();
        List<ShowProjectFile> checkFiles;
        UserInfo userInfo;
        ProjectFileManager fileManager;
        CheckLocalFileServices() 
        {
            checkFiles = new List<ShowProjectFile>();
            checkTimer = new Timer(600000);
            checkTimer.Elapsed += CheckTimer_Elapsed;
        }

        private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (null == fileManager)
                return;
            CheckAndUpdateData();
        }
        private void CheckAndUpdateData() 
        {
            lock (checkFiles)
            {
                if (checkFiles.Count < 1)
                    return;
                var rmList = new List<ShowProjectFile>();
                var addList = new List<ShowProjectFile>();
                foreach (var prjFile in checkFiles)
                {
                    var haveChange = CheckProjectFile(prjFile);
                    if (haveChange)
                    {
                        //修改本地监听的数据
                        rmList.Add(prjFile);
                        var newPrjFile = fileManager.GetProjectFile(prjFile.ProjectFileId);
                        if (null != newPrjFile)
                        {
                            addList.Add(newPrjFile);
                        }
                    }
                }
                foreach (var item in rmList)
                    checkFiles.Remove(item);
                foreach (var item in addList)
                    checkFiles.Add(item);
            }
        }
        private bool CheckProjectFile(ShowProjectFile prjFile) 
        {
            bool haveChange = false;
            foreach (var item in prjFile.FileInfos)
            {
                if (string.IsNullOrEmpty(item.FileLocalPath))
                    continue;
                if (!File.Exists(item.FileLocalPath))
                    continue;
                var fileMD5 = FileHelper.GetMD5ByMD5CryptoService(item.FileLocalPath);
                if (fileMD5 == item.FileMD5)
                    continue;
                //有改变，上传相应的文件
                if (!fileManager.UpdateProjectFile(item))
                    continue;
                haveChange = true;
            }
            //检查是否是第一次上传IFC文件
            if (prjFile.ApplcationName == EApplcationName.SU && null == prjFile.OpenFile)
            {
                var mainPath = Path.GetDirectoryName(prjFile.MainFile.FileLocalPath);
                var mainFileName = Path.GetFileNameWithoutExtension(prjFile.MainFile.FileLocalPath);
                var newIfcPath = Path.Combine(mainPath, mainFileName + ".ifc");
                var dir = Path.GetDirectoryName(prjFile.MainFile.FileDownloadPath);
                if (File.Exists(newIfcPath))
                {
                    FileDetail addFile = new FileDetail()
                    {
                        ProjectFileId = prjFile.ProjectFileId,
                        ProjectUploadId = System.Guid.NewGuid().ToString(),
                        FileLocalPath = newIfcPath,
                        FileDownloadPath = Path.Combine(dir, mainFileName + ".ifc"),
                        IsMainFile = false,
                    };
                    fileManager.UpdateProjectFile(addFile);
                    haveChange = true;
                }
            }
            return haveChange;
        }
        public void BindingUserInfo(UserInfo user) 
        {
            if (null != userInfo)
            {
                lock (userInfo)
                {
                    userInfo = user;
                }
            }
            else 
            {
                userInfo = user;
            }
            if (null == fileManager)
                fileManager = new ProjectFileManager(userInfo);
            else 
            {
                lock (fileManager)
                {
                    fileManager = new ProjectFileManager(userInfo);
                }
            }
            
        }
        public string ForceUpdateProjectFile(ShowProjectFile prjFile) 
        {
            if (null == fileManager)
                return "未初始化无法进行后续步骤";
            lock (checkFiles)
            {
                var haveChange = CheckProjectFile(prjFile);
                if (!haveChange)
                    return "无修改，无需上传";
                //检查是否在监听中，如果在监听中，刷新数据
                var rmList = new List<ShowProjectFile>();
                var addList = new List<ShowProjectFile>();
                foreach (var item in checkFiles) 
                {
                    if (item.ProjectFileId != prjFile.ProjectFileId)
                        continue;
                    rmList.Add(item);
                    var newPrjFile = fileManager.GetProjectFile(prjFile.ProjectFileId);
                    if (null != newPrjFile)
                    {
                        addList.Add(newPrjFile);
                    }
                }
                foreach (var item in rmList)
                    checkFiles.Remove(item);
                foreach (var item in addList)
                    checkFiles.Add(item);
            }
            return "更新成功";
        }
        public void AddCheckFile(ShowProjectFile projectFile) 
        {
            lock (checkFiles) 
            {
                if (checkFiles.Any(c => c.ProjectFileId == projectFile.ProjectFileId))
                    return;
                checkFiles.Add(projectFile);
            }
        }
        public void RemoveCheckFile(string key) 
        {
            if (string.IsNullOrEmpty(key))
                return;
            lock (checkFiles)
            {
                checkFiles = checkFiles.Where(c => c.ProjectFileId != key).ToList();
            }
        }
        public void ClearAllCheckFile() 
        {
            lock (checkFiles)
            {
                checkFiles.Clear();
            }
        }
        public void StartCheck() 
        {
            lock (checkTimer) 
            {
                if (checkTimer != null && !checkTimer.Enabled)
                    checkTimer.Start();
            }
        }
        public void StopCheck() 
        {
            lock (checkTimer)
            {
                if (checkTimer != null && checkTimer.Enabled)
                    checkTimer.Stop();
            }
        }
    }
}
