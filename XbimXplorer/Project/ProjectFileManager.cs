using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using THBimEngine.Common;
using THBimEngine.DBOperation;
using THBimEngine.DBOperation.DBModels;
using THBimEngine.Domain;
using THBimEngine.HttpService;
using XbimXplorer.ThBIMEngine;

namespace XbimXplorer.Project
{
    class ProjectFileManager
    {
        public ProjectFileHelper ProjectFileDBHelper;
        public ProjectDBHelper ProjectDBHelper;
        public string userId { get; }
        public string userName { get; }
        public string location { get; }
        FileHttp fileHttp;
        public ProjectFileManager(UserInfo user):this(user.UserLogin.Username,user.ChineseName,user.LoginLocation)
        {
        }
        public ProjectFileManager(string uId,string uName,string loginLocation) 
        {
            userId = uId;
            userName = uName;
            location = loginLocation;
            var ipConfig = IpConfigService.GetConfigByLocation(loginLocation);
            ProjectFileDBHelper = new ProjectFileHelper(Encryption.AesDecrypt(ipConfig.DBConnectString,""));
            ProjectDBHelper = new ProjectDBHelper(Encryption.AesDecrypt(ipConfig.XTDBConnectString,""));
            fileHttp = new FileHttp(ipConfig.FileServiceIP);
        }
        public bool FileLocalPathCheckAndDownload(FileDetail fileInfo,bool checkFileMD5) 
        {
            if (null == fileInfo || string.IsNullOrEmpty(fileInfo.FileDownloadPath))
                return false;
            var downloadUrl = fileInfo.FileDownloadPath;
            var localPath = fileInfo.FileLocalPath;
            var dir = Path.GetDirectoryName(localPath);
            ProjectCommon.CheckAndAddDir(dir);
            bool needDownload = true;
            if (checkFileMD5 && File.Exists(localPath)) 
            {
                var fileMD5 = FileHelper.GetMD5ByMD5CryptoService(localPath);
                if (fileMD5 == fileInfo.FileMD5)
                {
                    needDownload = false;
                }
                else 
                {
                    try 
                    {
                        File.Delete(localPath);
                    }
                    catch { }
                }
            }
            if (!needDownload)
                return true;
            if (!File.Exists(localPath)) 
            {
                fileHttp.DownloadFile(downloadUrl, localPath);
            }
            return File.Exists(localPath);
        }

        public List<ShowProjectFile> GetProjectFiles(string pPrjId)
        {
            var res = new List<ShowProjectFile>();
            //step1获取改项目的主文件
            var allMainFiles = ProjectFileDBHelper.GetProjectFiles(pPrjId);
            if (allMainFiles.Count < 1)
                return res;
            return GetProjectFiles(allMainFiles);
        }
        public List<ShowProjectFile> GetProjectFiles(ShowProject pPrj, ShowProject subPrj)
        {
            var res = new List<ShowProjectFile>();
            //step1获取改项目的主文件
            var allMainFiles = ProjectFileDBHelper.GetProjectFiles(pPrj.PrjId, subPrj.PrjId);
            if (allMainFiles.Count < 1)
                return res;
            return GetProjectFiles(allMainFiles);
        }
        public List<ShowProjectFile> GetProjectDeleteFiles(ShowProject pPrj, ShowProject subPrj) 
        {
            var res = new List<ShowProjectFile>();
            //step1获取改项目的主文件
            var allMainFiles = ProjectFileDBHelper.GetProjectDeleteFiles(pPrj.PrjId, subPrj.PrjId);
            if (allMainFiles.Count < 1)
                return res;
            return GetProjectDeleteFiles(allMainFiles);
        }
        public ShowProjectFile GetProjectFile(string prjFileId)
        {
            //step1获取改项目的主文件
            var mainFile = ProjectFileDBHelper.GetProjectFile(prjFileId);
            if (null == mainFile)
                return null;
            var res = GetProjectFiles(new List<DBVProjectMainFile> { mainFile });
            return res.FirstOrDefault();
        }
        public ShowProjectFile GetProjectFileByPath(string prjId,string subPrjId,string localMainFilePath)
        {
            var temp = localMainFilePath.ToUpper();
            var allMainFiles = ProjectFileDBHelper.GetProjectFiles(prjId, subPrjId);
            var allPrjFiles = GetProjectFiles(allMainFiles);
            return allPrjFiles.Where(c => c.MainFile.FileLocalPath.ToUpper() == temp).FirstOrDefault();
        }
        private List<ShowProjectFile> GetProjectFiles(List<DBVProjectMainFile> allMainFiles)
        {
            //外链信息为了保持最新，在双击时再获取信息
            var res = new List<ShowProjectFile>();
            if (allMainFiles.Count < 1)
                return res;
            //step2 获取项目主文件下的其它文件
            var allIds = allMainFiles.Select(c => c.ProjectFileId).ToList();
            var allFileUploadIds = ProjectFileDBHelper.GetProjectAllFileIds(allIds);
            var prjAllFiles = ProjectFileDBHelper.GetProjectAllFiles(allFileUploadIds.Select(c => c.ProjectFileUploadId).ToList());
            
            //step3 构造显示数据
            foreach (var mainFile in allMainFiles)
            {
                //一条记录可能会有多个文件，如（一个ydb文件，还对应一个ifc文件，可能还会有其它的文件信息）
                var addPrjFile = ProjectCommon.DBVProjectMainFileToProjectShowFile(mainFile);
                addPrjFile.FileInfos = new List<FileDetail>();
                var prjAllDBFiles = prjAllFiles.Where(c => c.ProjectFileId == mainFile.ProjectFileId).ToList();
                foreach (var item in prjAllDBFiles)
                {
                    var addFile = ProjectCommon.DBVProjectFileToFileDetail(item);
                    var path = Path.GetDirectoryName(addFile.FileDownloadPath);
                    addFile.FileLocalPath = Path.Combine(ProjectCommon.GetRootPath(location), Path.Combine(path, addFile.FileRealName));
                    addPrjFile.FileInfos.Add(addFile);
                }
                addPrjFile.MainFile = addPrjFile.FileInfos.Where(c => c.IsMainFile).FirstOrDefault();
                addPrjFile.MainFileId = addPrjFile.MainFile != null ? addPrjFile.MainFile.ProjectFileId : "";
                addPrjFile.LastUpdateTime = addPrjFile.MainFile.UploadTime;
                addPrjFile.OpenFile = addPrjFile.MainFile.CanOpen ? addPrjFile.MainFile : addPrjFile.FileInfos.Where(c => c.CanOpen).FirstOrDefault();
                res.Add(addPrjFile);
            }
            return res;
        }
        private List<ShowProjectFile> GetProjectDeleteFiles(List<DBVProjectMainDelFile> allDelFiles)
        {
            //外链信息为了保持最新，在双击时再获取信息
            var res = new List<ShowProjectFile>();
            if (allDelFiles.Count < 1)
                return res;
            //step2 获取项目主文件下的其它文件
            var allIds = allDelFiles.Select(c => c.ProjectFileId).ToList();
            var allFileUploadIds = ProjectFileDBHelper.GetProjectAllFileIds(allIds);
            var prjAllFiles = ProjectFileDBHelper.GetProjectAllFiles(allFileUploadIds.Select(c => c.ProjectFileUploadId).ToList());
            //step3 构造显示数据
            foreach (var mainFile in allDelFiles)
            {
                //一条记录可能会有多个文件，如（一个ydb文件，还对应一个ifc文件，可能还会有其它的文件信息）
                var addPrjFile = ProjectCommon.DBVProjectMainFileToProjectShowFile(mainFile);
                addPrjFile.FileInfos = new List<FileDetail>();
                var prjAllDBFiles = prjAllFiles.Where(c => c.ProjectFileId == mainFile.ProjectFileId).ToList();
                foreach (var item in prjAllDBFiles)
                {
                    var addFile = ProjectCommon.DBVProjectFileToFileDetail(item);
                    var path = Path.GetDirectoryName(addFile.FileDownloadPath);
                    addFile.FileLocalPath = Path.Combine(ProjectCommon.GetRootPath(location), Path.Combine(path, addFile.FileRealName));
                    addPrjFile.FileInfos.Add(addFile);
                }
                res.Add(addPrjFile);
            }
            return res;
        }
        public bool UpdateProjectFile(ShowProjectFile projectFileInfo,string selectFilePath) 
        {
            bool isSuccess = true;
            var uploadFiles = new List<TempFileInfo>();
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                var fileMD5 = FileHelper.GetMD5ByMD5CryptoService(selectFilePath);
                if (fileMD5 == projectFileInfo.MainFile.FileMD5) 
                {
                    MessageBox.Show("选中的新文件和之前文件相同，不进行更新操作", "操作提醒", MessageBoxButton.OK);
                    return false;
                }
                var dir = Path.GetDirectoryName(projectFileInfo.MainFile.FileDownloadPath);
                var currentFilePath = projectFileInfo.MainFile.FileLocalPath;
                var oldFileNameNoExt = Path.GetFileNameWithoutExtension(currentFilePath);
                var oldFileName = Path.GetFileName(currentFilePath);
                var currentDir = Path.GetDirectoryName(currentFilePath);
                var extName = Path.GetExtension(selectFilePath).ToLower();
                var newName = System.Guid.NewGuid().ToString();
                var guidFileName = string.Format("{0}{1}", newName, extName);
                var newFilePath = Path.Combine(currentDir, guidFileName);
                var isIfc = extName.Contains("ifc");
                uploadFiles.Add(new TempFileInfo
                {
                    FilePath = newFilePath,
                    FileRealName = oldFileName,
                    IsMain = true,
                    NeedDownload = isIfc,
                });
                //uploadFiles.Add(newFilePath, oldFileName);
                File.Copy(selectFilePath, newFilePath, true);
                if (projectFileInfo.ApplcationName == EApplcationName.YDB)
                {
                    ThYDBToIfcConvertService ydbToIfc = new ThYDBToIfcConvertService();
                    ydbToIfc.Convert(newFilePath);
                    var tempIfc = Path.Combine(currentDir, string.Format("{0}{1}", newName, ".ifc"));
                    uploadFiles.Add(new TempFileInfo
                    {
                        FilePath = tempIfc,
                        FileRealName = string.Format("{0}{1}", oldFileNameNoExt, ".ifc"),
                        IsMain = false,
                        NeedDownload = true
                    });
                    uploadFiles.Add(new TempFileInfo
                    {
                        FilePath = tempIfc+".json",
                        FileRealName = string.Format("{0}.ifc.json", oldFileNameNoExt),
                        IsMain = false,
                        NeedDownload = true
                    });
                }
                //step2 上传文件 ,插入数据库，判断是新记录还旧记录
                //需要插入文件上传信息，项目文件信息
                var versionId = Guid.NewGuid().ToString();
                string prjFileId = projectFileInfo.ProjectFileId;
                var addDBFiles = new List<DBFile>();
                var addProjectUploads = new List<DBProjectFileUpload>();
                foreach (var item in uploadFiles)
                {
                    var webFileName = Path.GetFileName(item.FilePath);
                    fileHttp.UploadFile(item.FilePath, webFileName, dir);
                    //插入文件上传记录
                    var addDBFile = new DBFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        FileUrl = string.Format("{0}\\{1}", dir, webFileName),
                        FileRealName = item.FileRealName,
                        Uploader = userId,
                        UploaderName = userName,
                        FileMD5 = FileHelper.GetMD5ByMD5CryptoService(item.FilePath),
                    };
                    addDBFiles.Add(addDBFile);
                    //第一个文件肯定是主文件, 插入项目文件上传记录
                    var canOpen = Path.GetExtension(item.FilePath).ToLower().Contains("ifc");
                    var addPrjFileUplaod = new DBProjectFileUpload
                    {
                        ProjectFileUploadId = Guid.NewGuid().ToString(),
                        FileName = Path.GetFileName(item.FileRealName),
                        IsMainFile = item.IsMain ? 1 : 0,
                        ProjectFileId = prjFileId,
                        FileUploadId = addDBFile.FileId,
                        CanOpen = canOpen ? 1 : 0,
                        NeedDownload = item.NeedDownload?1:0,
                        VersionId = versionId,
                    };
                    addProjectUploads.Add(addPrjFileUplaod);
                }
                sqlDB.Ado.BeginTran();
                if (projectFileInfo.ApplcationName == EApplcationName.SU)
                {
                    //su只删除主文件
                    ProjectFileDBHelper.DelHisProjectUploadFile(sqlDB, projectFileInfo.MainFile.ProjectUploadId);
                }
                else 
                {
                    ProjectFileDBHelper.DelHisProjectAllUploadFile(sqlDB, prjFileId);
                }
                
                foreach (var item in addDBFiles)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                foreach (var item in addProjectUploads)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                sqlDB.Ado.CommitTran();
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("上传数据到服务器失败，{0}", ex.Message), "操作提醒");
            }
            finally
            {
                if (isSuccess)
                {
                    //上传成功，修改文件名称
                    foreach (var item in uploadFiles)
                    {
                        var oldPath = item.FilePath;
                        var dir = Path.GetDirectoryName(oldPath);
                        var newPath = Path.Combine(dir, item.FileRealName);
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(oldPath, newPath);
                    }
                }
                else
                {
                    //上传失败，删除相应的文件
                    foreach (var item in uploadFiles)
                    {
                        if (File.Exists(item.FilePath))
                            File.Delete(item.FilePath);
                    }
                }
            }
            return isSuccess;
        }
        public bool UpdateProjectFile(FileDetail projectFileInfo)
        {
            bool isSuccess = true;
            var uploadFiles = new List<TempFileInfo>();
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                var currentFilePath = projectFileInfo.FileLocalPath;
                var dir = Path.GetDirectoryName(projectFileInfo.FileDownloadPath);
                var currentDir = Path.GetDirectoryName(currentFilePath);
                var extName = Path.GetExtension(currentFilePath);
                var oldFileNameNoExt = Path.GetFileNameWithoutExtension(currentFilePath);
                var oldFileName = Path.GetFileName(currentFilePath);
                var newName = System.Guid.NewGuid().ToString();
                var guidFileName = string.Format("{0}{1}", newName, extName);
                var newFilePath = Path.Combine(currentDir, guidFileName);
                //uploadFiles.Add(newFilePath, currentFilePath);
                var isIfc = extName.Contains("ifc");
                uploadFiles.Add(new TempFileInfo
                {
                    FilePath = newFilePath,
                    FileRealName = oldFileName,
                    IsMain = projectFileInfo.IsMainFile,
                    NeedDownload = isIfc,
                });
                File.Copy(currentFilePath, newFilePath, true);
                //step2 上传文件 ,插入数据库，判断是新记录还旧记录
                //需要插入文件上传信息，项目文件信息
                string prjFileId = projectFileInfo.ProjectFileId;
                var addDBFiles = new List<DBFile>();
                var addProjectUploads = new List<DBProjectFileUpload>();
                var temp = uploadFiles.FirstOrDefault();
                var webFileName = Path.GetFileName(temp.FilePath);
                fileHttp.UploadFile(temp.FilePath, webFileName, dir);
                //插入文件上传记录
                var addDBFile = new DBFile
                {
                    FileId = Guid.NewGuid().ToString(),
                    FileUrl = string.Format("{0}\\{1}", dir, webFileName),
                    FileRealName = temp.FileRealName,
                    Uploader = userId,
                    UploaderName = userName,
                    FileMD5 = FileHelper.GetMD5ByMD5CryptoService(temp.FilePath),
                };
                addDBFiles.Add(addDBFile);
                var canOpen = Path.GetExtension(temp.FilePath).ToLower().Contains("ifc");
                var addPrjFileUplaod = new DBProjectFileUpload
                {
                    ProjectFileUploadId = Guid.NewGuid().ToString(),
                    FileName = Path.GetFileName(temp.FileRealName),
                    IsMainFile = temp.IsMain ? 1 : 0,
                    ProjectFileId = prjFileId,
                    FileUploadId = addDBFile.FileId,
                    CanOpen = canOpen ? 1 : 0,
                    NeedDownload = temp.NeedDownload ? 1 : 0,
                };
                addProjectUploads.Add(addPrjFileUplaod);
                sqlDB.Ado.BeginTran();
                //这里只删除一个文件，就只改一个记录
                ProjectFileDBHelper.DelHisProjectUploadFile(sqlDB, projectFileInfo.ProjectFileId, addDBFile.FileRealName);
                foreach (var item in addDBFiles)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                foreach (var item in addProjectUploads)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                sqlDB.Ado.CommitTran();
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("上传数据到服务器失败，{0}", ex.Message), "操作提醒");
            }
            finally
            {
                foreach (var item in uploadFiles)
                {
                    if (File.Exists(item.FilePath))
                        File.Delete(item.FilePath);
                }
            }
            return isSuccess;
        }
        /// <summary>
        /// 新增项目文件操作（null 取消操作，true 操作成功，false 操作失败）
        /// </summary>
        /// <param name="pPrj"></param>
        /// <param name="subPrj"></param>
        /// <param name="filePath"></param>
        /// <param name="majorName"></param>
        /// <param name="typeName"></param>
        /// <param name="isCopy"></param>
        /// <returns></returns>
        public bool? AddFileToProject(ShowProject pPrj, ShowProject subPrj, string filePath, string majorName, string typeName, bool isCopy)
        {
            bool isSuccess = false;
            var oldFileName = Path.GetFileName(filePath);
            var oldFileNameNoExt = Path.GetFileNameWithoutExtension(oldFileName);
            var extName = Path.GetExtension(filePath).ToLower();
            var newName = System.Guid.NewGuid().ToString();
            var guidFileName = string.Format("{0}{1}", newName, extName);
            var path = ProjectCommon.GetProjectSubDir(pPrj, subPrj, location, majorName, typeName, true);
            var checkFilePath = Path.Combine(path, oldFileName);
            var newFilePath = Path.Combine(path, guidFileName);
            if (isCopy)
            {
                if (File.Exists(checkFilePath))
                {
                    var res = MessageBox.Show("已经有一个该名称的文件，继续上传将替换原有的文件，是否继续上传操作？", "操作提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes)
                        return null;
                }
            }
            var uploadFiles = new List<TempFileInfo>();
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                var dir = ProjectCommon.GetProjectSubDir(pPrj, subPrj, location, majorName, typeName, false);
                //step1 复制到本地
                DBProjectFile projectFile = null;
                var isIfc = extName.Contains("ifc");
                if (isCopy)
                {
                    uploadFiles.Add(new TempFileInfo
                    {
                        FilePath = newFilePath,
                        FileRealName = oldFileName,
                        IsMain = true,
                        NeedDownload = isIfc,
                    });
                    File.Copy(filePath, newFilePath, true);
                    if (typeName == "YDB" && extName.Contains(".ydb"))
                    {
                        ThYDBToIfcConvertService ydbToIfc = new ThYDBToIfcConvertService();
                        ydbToIfc.Convert(newFilePath);
                        var tempIfc = Path.Combine(path, string.Format("{0}{1}", newName, ".ifc"));
                        uploadFiles.Add(new TempFileInfo
                        {
                            FilePath = tempIfc,
                            FileRealName = string.Format("{0}{1}", oldFileNameNoExt, ".ifc"),
                            IsMain = false,
                            NeedDownload = true
                        });
                        uploadFiles.Add(new TempFileInfo
                        {
                            FilePath = tempIfc + ".json",
                            FileRealName = string.Format("{0}.ifc.json", oldFileNameNoExt),
                            IsMain = false,
                            NeedDownload = true
                        });
                    }
                }
                else
                {
                    //重命名
                    File.Move(filePath, newFilePath);
                    uploadFiles.Add(new TempFileInfo
                    {
                        FilePath = newFilePath,
                        FileRealName = oldFileName,
                        IsMain = true,
                        NeedDownload = isIfc,
                    });
                }
                //step2 上传文件 ,插入数据库，判断是新记录还旧记录
                projectFile = ProjectFileDBHelper.GetHisProjectFile(pPrj.PrjId, subPrj.PrjId, majorName, typeName, oldFileNameNoExt, "");
                //需要插入文件上传信息，项目文件信息
                string prjFileId = System.Guid.NewGuid().ToString();
                string versionId = Guid.NewGuid().ToString();
                bool haveHis = false;
                if (null != projectFile)
                {
                    //已有记录需要更新旧的记录，再插入新的数据
                    prjFileId = projectFile.ProjectFileId;
                    haveHis = true;
                    
                }
                var addDBFiles = new List<DBFile>();
                var addProjectUploads = new List<DBProjectFileUpload>();
                foreach (var item in uploadFiles)
                {
                    var webFileName = Path.GetFileName(item.FilePath);
                    fileHttp.UploadFile(item.FilePath, webFileName, dir);
                    //插入文件上传记录
                    var addDBFile = new DBFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        FileUrl = string.Format("{0}\\{1}", dir, webFileName),
                        FileRealName = item.FileRealName,
                        Uploader = userId,
                        UploaderName = userName,
                        FileMD5 = FileHelper.GetMD5ByMD5CryptoService(item.FilePath),
                    };
                    addDBFiles.Add(addDBFile);
                    var canOpen = Path.GetExtension(item.FilePath).ToLower().Contains("ifc");
                    var addPrjFileUplaod = new DBProjectFileUpload
                    {
                        ProjectFileUploadId = Guid.NewGuid().ToString(),
                        FileName = Path.GetFileName(item.FileRealName),
                        IsMainFile = item.IsMain ? 1 : 0,
                        ProjectFileId = prjFileId,
                        FileUploadId = addDBFile.FileId,
                        CanOpen = canOpen ? 1 : 0,
                        NeedDownload =item.NeedDownload?1:0,
                        VersionId = versionId,
                    };
                    addProjectUploads.Add(addPrjFileUplaod);
                }

                var addDBProjetFile = new DBProjectFile()
                {
                    ProjectFileId = prjFileId,
                    PrjId = pPrj.PrjId,
                    PrjName = pPrj.ShowName,
                    SubPrjId = subPrj.PrjId,
                    SubPrjName = subPrj.ShowName,
                    FileName = oldFileNameNoExt,
                    ApplicationName = typeName,
                    MajorName = majorName,
                    CreaterId = userId,
                    CreaterName = userName,
                };
                sqlDB.Ado.BeginTran();
                //如果已经有历史了，不删除原来的主记录，也不插入新记录
                if (haveHis)
                {
                    ProjectFileDBHelper.DelHisProjectAllUploadFile(sqlDB, prjFileId);
                }
                else 
                {
                    sqlDB.Insertable(addDBProjetFile).ExecuteCommand();
                }
                foreach (var item in addDBFiles)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                foreach (var item in addProjectUploads)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                sqlDB.Ado.CommitTran();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("上传数据到服务器失败，{0}", ex.Message), "操作提醒");
            }
            finally
            {
                if (isSuccess)
                {
                    //上传成功，修改文件名称
                    foreach (var item in uploadFiles)
                    {
                        var oldPath = item.FilePath;
                        var dir = Path.GetDirectoryName(oldPath);
                        var newPath = Path.Combine(dir, item.FileRealName);
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(oldPath, newPath);
                    }
                }
                else
                {
                    //上传失败，删除相应的文件
                    foreach (var item in uploadFiles)
                    {
                        if (File.Exists(item.FilePath))
                            File.Delete(item.FilePath);
                    }
                }
            }
            return isSuccess;
        }
        /// <summary>
        /// 给项目主文件加入其它文件（如 -100%.ifc)
        /// 这里加入的文件都是不打开的，如果一个主文件有多个可以打开的子文件，程序会随机打开一个，导致显示错误
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="mainFileId"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public string AddFileToProjectFile(ProjectInfo projectInfo, string mainFileId, string filePath,string major,string type,bool needDownload,bool isCopy)
        {
            string res = "";
            var oldFileName = Path.GetFileName(filePath);
            var extName = Path.GetExtension(filePath).ToLower();
            var newName = System.Guid.NewGuid().ToString();
            var guidFileName = string.Format("{0}{1}", newName, extName);
            var path = ProjectCommon.GetProjectSubDir(projectInfo, location, major,type, true);
            var newFilePath = Path.Combine(path, guidFileName);
            var uploadFiles = new List<TempFileInfo>();
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                var dir = ProjectCommon.GetProjectSubDir(projectInfo, location, major, type, false);
                if (isCopy)
                {
                    uploadFiles.Add(new TempFileInfo
                    {
                        FilePath = newFilePath,
                        FileRealName = oldFileName,
                        IsMain = false,
                        NeedDownload = needDownload,
                    });
                    File.Copy(filePath, newFilePath, true);
                }
                else
                {
                    //重命名
                    File.Move(filePath, newFilePath);
                    uploadFiles.Add(new TempFileInfo
                    {
                        FilePath = newFilePath,
                        FileRealName = oldFileName,
                        IsMain = false,
                        NeedDownload = needDownload,
                    });
                }
                var addDBFiles = new List<DBFile>();
                var addProjectUploads = new List<DBProjectFileUpload>();
                foreach (var item in uploadFiles)
                {
                    var webFileName = Path.GetFileName(item.FilePath);
                    fileHttp.UploadFile(item.FilePath, webFileName, dir);
                    //插入文件上传记录
                    var addDBFile = new DBFile
                    {
                        FileId = Guid.NewGuid().ToString(),
                        FileUrl = string.Format("{0}\\{1}", dir, webFileName),
                        FileRealName = item.FileRealName,
                        Uploader = userId,
                        UploaderName = userName,
                        FileMD5 = FileHelper.GetMD5ByMD5CryptoService(item.FilePath),
                    };
                    addDBFiles.Add(addDBFile);
                    var canOpen = Path.GetExtension(item.FilePath).ToLower().Contains("ifc");
                    var addPrjFileUplaod = new DBProjectFileUpload
                    {
                        ProjectFileUploadId = Guid.NewGuid().ToString(),
                        FileName = Path.GetFileName(item.FileRealName),
                        IsMainFile = item.IsMain ? 1 : 0,
                        ProjectFileId = mainFileId,
                        FileUploadId = addDBFile.FileId,
                        CanOpen = 0,
                        NeedDownload = item.NeedDownload ? 1 : 0,
                    };
                    addProjectUploads.Add(addPrjFileUplaod);
                }
                sqlDB.Ado.BeginTran();
                ProjectFileDBHelper.DelHisProjectUploadFile(sqlDB, mainFileId, oldFileName);
                foreach (var item in addDBFiles)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                foreach (var item in addProjectUploads)
                {
                    sqlDB.Insertable(item).ExecuteCommand();
                }
                sqlDB.Ado.CommitTran();
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                res = string.Format("上传数据到服务器失败，{0}", ex.Message);
            }
            finally 
            {
                if (string.IsNullOrEmpty(res))
                {
                    //上传成功，修改文件名称
                    foreach (var item in uploadFiles)
                    {
                        var oldPath = item.FilePath;
                        var dir = Path.GetDirectoryName(oldPath);
                        var newPath = Path.Combine(dir, item.FileRealName);
                        if (File.Exists(newPath))
                            File.Delete(newPath);
                        File.Move(oldPath, newPath);
                    }
                }
            }
            return res;
        }
        /// <summary>
        /// 删除操作（null 取消操作，true 删除成功,false 删除失败）
        /// </summary>
        /// <param name="pPrj"></param>
        /// <param name="subPrj"></param>
        /// <param name="projectFileInfo"></param>
        /// <returns></returns>
        public bool? ProjectFileDelete(ShowProject pPrj, ShowProject subPrj, ShowProjectFile projectFileInfo)
        {
            var strMsg = string.Format("确定要作废 {0} 下的 {1} 专业的 {2} 文件吗，该过程是不可逆的，是否继续操作？", subPrj.ShowName, projectFileInfo.MajorName, projectFileInfo.ShowFileName);
            var res = MessageBox.Show(strMsg, "操作提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes)
                return null;
            bool opRes = false;
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                //删除本地的文件
                foreach (var fileInfo in projectFileInfo.FileInfos)
                {
                    if (!string.IsNullOrEmpty(fileInfo.FileLocalPath) && File.Exists(fileInfo.FileLocalPath))
                    {
                        File.Delete(fileInfo.FileLocalPath);
                    }
                }
                //删除数据库中相应的记录，只需要将主记录，
                //其它数据不动，如果后续还原只需要将该记录的状态修改即可，如果其它数据一起修改，还原时不知道其它数据要还原那些
                //只要主数据没有，后续的外链和打开都不会在打开这条记录了
                sqlDB.Ado.BeginTran();
                ProjectFileDBHelper.DelHisProjectFile(sqlDB, userId, userName, projectFileInfo.ProjectFileId);
                sqlDB.Ado.CommitTran();
                opRes = true;
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                opRes = false;
                MessageBox.Show(string.Format("作废失败，{0}", ex.Message), "操作提醒");
            }
            return opRes;
        }

        #region 外链相关
        public List<ShowFileLink> GetMainFileLinkInfo(List<string> prjMainFileIds)
        {
            var res = new List<ShowFileLink>();
            if (null == prjMainFileIds || prjMainFileIds.Count < 1)
                return res;
            var allLinks = ProjectFileDBHelper.GetProjectFileLink(prjMainFileIds);
            var otherFileIds = allLinks.Select(c => c.LinkProjectFileId).ToList();
            var otherMainFiles = ProjectFileDBHelper.GetProjectFiles(otherFileIds);
            var allFileUploadIds = ProjectFileDBHelper.GetProjectAllFileIds(otherFileIds);
            var prjAllFiles = ProjectFileDBHelper.GetProjectAllFiles(allFileUploadIds.Select(c => c.ProjectFileUploadId).ToList());
            foreach (var link in allLinks)
            {
                var showLink = ProjectCommon.DBLinkToShowLink(link);
                var linkFile = otherMainFiles.Where(c => c.ProjectFileId == link.LinkProjectFileId).FirstOrDefault();
                if (linkFile == null)
                    continue;
                var addPrjFile = ProjectCommon.DBVProjectMainFileToProjectShowFile(linkFile);
                addPrjFile.FileInfos = new List<FileDetail>();
                var prjAllDBFiles = prjAllFiles.Where(c => c.ProjectFileId == linkFile.ProjectFileId).ToList();
                foreach (var item in prjAllDBFiles)
                {
                    var addFile = ProjectCommon.DBVProjectFileToFileDetail(item);
                    var path = Path.GetDirectoryName(addFile.FileDownloadPath);
                    addFile.FileLocalPath = Path.Combine(ProjectCommon.GetRootPath(location), Path.Combine(path, addFile.FileRealName));
                    addPrjFile.FileInfos.Add(addFile);
                }
                addPrjFile.MainFile = addPrjFile.FileInfos.Where(c => c.IsMainFile).FirstOrDefault();
                addPrjFile.MainFileId = addPrjFile.MainFile != null ? addPrjFile.MainFile.ProjectFileId : "";
                addPrjFile.LastUpdateTime = addPrjFile.MainFile.UploadTime;
                addPrjFile.OpenFile = addPrjFile.MainFile.CanOpen ? addPrjFile.MainFile : addPrjFile.FileInfos.Where(c => c.CanOpen).FirstOrDefault();
                showLink.LinkProject = addPrjFile;
                res.Add(showLink);
            }
            return res;
        }
        public bool AddFileLink(ShowFileLink fileLink,bool isReturnLink)
        {
            bool isSuccess = false;
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                sqlDB.Ado.BeginTran();
                var addLinkItem = new DBLink
                {
                    LinkId = fileLink.LinkId,
                    FromLinkId = "",
                    ProjectFileId = fileLink.ProjectFileId,
                    LinkProjectFileId = fileLink.LinkProjectFileId,
                    LinkMoveX = fileLink.MoveX,
                    LinkMoveY = fileLink.MoveY,
                    LinkMoveZ = fileLink.MoveZ,
                    LinkRotainAngle = fileLink.RotainAngle,
                    LinkerId = userId,
                    LinkerName = userName,
                    IsDel = 0,
                };
                if (isReturnLink) 
                {
                    var returnLink = new DBLink
                    {
                        LinkId = System.Guid.NewGuid().ToString(),
                        FromLinkId = addLinkItem.LinkId,
                        ProjectFileId = fileLink.LinkProjectFileId,
                        LinkProjectFileId = fileLink.ProjectFileId,
                        LinkMoveX = -fileLink.MoveX,
                        LinkMoveY = -fileLink.MoveY,
                        LinkMoveZ = -fileLink.MoveZ,
                        LinkRotainAngle = -fileLink.RotainAngle,
                        LinkerId = userId,
                        LinkerName = userName,
                        IsDel = 0,
                    };
                    fileLink.FromLinkId = returnLink.LinkId;
                    addLinkItem.FromLinkId = returnLink.LinkId;
                    sqlDB.Insertable(returnLink).ExecuteCommand();
                }
                sqlDB.Insertable(addLinkItem).ExecuteCommand();
                sqlDB.Ado.CommitTran();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("新增外链失败,{0}", ex.Message), "操作提醒", MessageBoxButton.OK);
                
            }
            return isSuccess;
        }
        public bool UpdateFileLink(ShowFileLink fileLink)
        {
            bool isSuccess = false;
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                sqlDB.Ado.BeginTran();
                var linkId = fileLink.LinkId;
                sqlDB.Updateable<DBLink>().SetColumns(it => 
                    new DBLink()
                    {
                        LinkMoveX = fileLink.MoveX,
                        LinkMoveY = fileLink.MoveY,
                        LinkMoveZ = fileLink.MoveZ,
                        LinkRotainAngle = fileLink.RotainAngle,
                    })
                    .Where(it => it.LinkId == linkId).ExecuteCommand();
                var tempLinkId = fileLink.FromLinkId;
                if (!string.IsNullOrEmpty(tempLinkId)) 
                {
                    var test = fileLink.GetLinkMatrix3D;
                    test.Invert();
                    //test.Right.Angle(test.Forward);
                    double tempX = test.OffsetX,
                    tempY = test.OffsetY,
                    tempZ = test.OffsetZ,
                    tempRotaion = - fileLink.RotainAngle;
                    sqlDB.Updateable<DBLink>().SetColumns(it => 
                        new DBLink()
                        {
                            LinkMoveX = tempX,
                            LinkMoveY = tempY,
                            LinkMoveZ = tempZ,
                            LinkRotainAngle = tempRotaion,
                        }).Where(it => it.LinkId == tempLinkId).ExecuteCommand();
                }
                sqlDB.Ado.CommitTran();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("新增外链失败,{0}", ex.Message), "操作提醒", MessageBoxButton.OK);

            }
            return isSuccess;
        }
        public bool ChangeLinkState(ShowFileLink fileLink, bool isLoad,bool isDel) 
        {
            bool isSuccess = false;
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                sqlDB.Ado.BeginTran();
                string changeLinkId = fileLink.LinkId;
                if (isDel)
                {
                    sqlDB.Updateable<DBLink>().SetColumns(it => 
                        new DBLink()
                        {
                            IsDel = 1,
                        })
                        .Where(it => it.LinkId == changeLinkId).ExecuteCommand();
                }
                else 
                {
                    var linkId = fileLink.LinkId;
                    int linkState = isLoad ? 0 : 1;
                    sqlDB.Updateable<DBLink>().SetColumns(it =>
                        new DBLink()
                        {
                            LinkState = linkState,
                        })
                        .Where(it => it.LinkId == linkId).ExecuteCommand();
                }
                
                sqlDB.Ado.CommitTran();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("修改外链失败,{0}", ex.Message), "操作提醒", MessageBoxButton.OK);
            }
            return isSuccess;
        }
        #endregion

        #region 历史记录相关
        public List<FileHistory> GetProjectFileHistory(string prjId,string subPrjId,string appName,string majorName) 
        {
            var res = new List<FileHistory>();
            //获取文件文件和其对应的所有主记录
            var allRecords = ProjectFileDBHelper.GetDBProjectFiles(prjId, subPrjId, appName, majorName);
            if (allRecords == null || allRecords.Count < 1)
                return res;
            var allHisMainFileUplaods = ProjectFileDBHelper.GetDBProjectMainFiles(allRecords.Select(c => c.ProjectFileId).ToList());
            //构造数据
            foreach (var mainItem in allRecords) 
            {
                var mainHis = ProjectCommon.DBProjectFileToFileHistory(mainItem);
                mainHis.NewState = mainHis.State;
                //获取对应的历史，并按时间排序
                var hisFiles = allHisMainFileUplaods.Where(c => c.ProjectFileId == mainItem.ProjectFileId).OrderBy(c => c.UploadTime).ToList();
                foreach (var fileItem in hisFiles) 
                {
                    var his = ProjectCommon.DBVFileInfoToFileDetail(fileItem);
                    his.ShowFileName = mainHis.MainFileName;
                    his.State = mainHis.State;
                    his.NewState = mainHis.State;
                    his.IsCurrentVersion = fileItem.IsDel == "0";
                    mainHis.FileHistoryDetails.Add(his);
                    if (his.IsCurrentVersion)
                    {
                        mainHis.OldMainFileUplaodId = his.ProjectFileUplaodId;
                        mainHis.NewMainFileUplaodId = his.ProjectFileUplaodId;
                    }
                }
                res.Add(mainHis);
            }
            return res;
        }
        public bool ChangeNewFileInfoToDB(List<FileHistory> changeFileInfos,EApplcationName applcationName)
        {
            bool isSuccess = false;
            var sqlDB = ProjectFileDBHelper.GetDBConnect();
            try
            {
                sqlDB.Ado.BeginTran();
                foreach (var item in changeFileInfos) 
                {
                    if (item.State != item.NewState) 
                    {
                        //取消作废
                        ProjectFileDBHelper.UnDelHProjectFile(sqlDB, userId, userName, item.MainFileId);
                    }
                    if (item.OldMainFileUplaodId != item.NewMainFileUplaodId) 
                    {
                        //更改了版本
                        if (applcationName == EApplcationName.YDB)
                        {
                            //YDB对应的有多个文件，需要改多个数据(暂时没有实现)
                            var oldFile = item.FileHistoryDetails.Where(c => c.ProjectFileUplaodId == item.OldMainFileUplaodId).FirstOrDefault();
                            var newFile = item.FileHistoryDetails.Where(c => c.ProjectFileUplaodId == item.NewMainFileUplaodId).FirstOrDefault();
                            ProjectFileDBHelper.DelHisProjectByVersionUploadFile(sqlDB, item.MainFileId,oldFile.ProjectFileUploadVersionId);
                            ProjectFileDBHelper.UnDelHisProjectByVersionUploadFile(sqlDB, item.MainFileId, newFile.ProjectFileUploadVersionId);
                        }
                        else
                        {
                            ProjectFileDBHelper.DelHisProjectUploadFile(sqlDB, item.OldMainFileUplaodId);
                            ProjectFileDBHelper.UnDelProjectUploadFile(sqlDB, item.NewMainFileUplaodId);
                        }
                    }
                }
                sqlDB.Ado.CommitTran();
                isSuccess = true;
            }
            catch (Exception ex)
            {
                sqlDB.Ado.RollbackTran();
                isSuccess = false;
                MessageBox.Show(string.Format("新增外链失败,{0}", ex.Message), "操作提醒", MessageBoxButton.OK);

            }
            return isSuccess;
        }
        #endregion
    }
    class TempFileInfo 
    {
        public string FilePath { get; set; }
        public string FileRealName { get; set; }
        public bool IsMain { get; set; }
        public bool NeedDownload { get; set; }
    }
}
