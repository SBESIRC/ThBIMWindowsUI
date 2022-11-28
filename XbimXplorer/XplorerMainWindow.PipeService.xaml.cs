using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using THBimEngine.Application;
using THBimEngine.Domain;
using THBimEngine.Presention;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using XbimXplorer.Extensions.ModelMerge;
using XbimXplorer.ThBIMEngine;
using THBimEngine.DBOperation;
using XbimXplorer.Project;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        private ProtobufMessage thMessage = null;
        private ProtobufMessage suMessage = null;
        private StreamParameter streamParameter = null;
        private string ifc_ProjectPath = string.Empty;
        NamedPipeServerStream pipeServer = null;
        NamedPipeServerStream SU_pipeServer = null;
        NamedPipeServerStream ifc_pipeServer = null;
        NamedPipeServerStream file_pipeServer = null;
        BackgroundWorker backgroundWorker = null;
        BackgroundWorker SU_backgroundWorker = null;
        BackgroundWorker ifc_backgroundWorker = null;
        BackgroundWorker file_backgroundWorker = null;
        BackgroundWorker cutData_backgroundWork = null;
        BackgroundWorker cutData_backgroundWork2 = null;
        private void InitPipeService() 
        {
            pipeServer = new NamedPipeServerStream("THCAD2P3DPIPE", PipeDirection.In);
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += Background_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync();

            SU_pipeServer = new NamedPipeServerStream("THSU2P3DIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            SU_backgroundWorker = new BackgroundWorker();
            SU_backgroundWorker.DoWork += SU_Background_DoWork;
            SU_backgroundWorker.RunWorkerCompleted += SU_BackgroundWorker_RunWorkerCompleted;
            SU_backgroundWorker.RunWorkerAsync();

            ifc_pipeServer = new NamedPipeServerStream("THIFCSTREAM2P3DIPE", PipeDirection.In);
            ifc_backgroundWorker = new BackgroundWorker();
            ifc_backgroundWorker.DoWork += ifc_Background_DoWork;
            ifc_backgroundWorker.RunWorkerCompleted += ifc_BackgroundWorker_RunWorkerCompleted;
            ifc_backgroundWorker.RunWorkerAsync();

            file_pipeServer = new NamedPipeServerStream("THIFCFILE2P3DPIE", PipeDirection.In);
            file_backgroundWorker = new BackgroundWorker();
            file_backgroundWorker.DoWork += file_Background_DoWork;
            file_backgroundWorker.RunWorkerCompleted += file_BackgroundWorker_RunWorkerCompleted;
            file_backgroundWorker.RunWorkerAsync();

            cutData_backgroundWork = new BackgroundWorker();
            cutData_backgroundWork.DoWork += CutData_backgroundWork_DoWork;
            cutData_backgroundWork.RunWorkerCompleted += CutData_backgroundWork_RunWorkerCompleted;
            cutData_backgroundWork.RunWorkerAsync();
        }

        private void CutData_backgroundWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
            cutData_backgroundWork.RunWorkerAsync();
        }

        private void CutData_backgroundWork_DoWork(object sender, DoWorkEventArgs e)
        {
            InitMutex();
            try
            {
                string getFileType = "";

               
                while (true)
                {
                    var flag = Mutex.TryOpenExisting("cadMutex", out CadMutex);
                    if (flag) break;
                    Thread.Sleep(100);
                }
                while (true)
                {
                    var flag = Mutex.TryOpenExisting("viewerMutex2", out ViewerMutex2);
                    if (flag) break;
                    Thread.Sleep(100);
                }
                ViewerMutex2.WaitOne();
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("getFileType"))
                {
                    using (MemoryMappedViewStream stream = mmf.CreateViewStream(0L, 0L, MemoryMappedFileAccess.Read))
                    {
                        IFormatter formatter = new BinaryFormatter();
                        getFileType = (string)formatter.Deserialize(stream);
                    }
                }
                FlagMutex.ReleaseMutex();

                var prjName = CurrentDocument.AllBimProjects.First().ProjectIdentity.Split('.').First() + "-100%.ifc";

                var fileName = Path.Combine(System.IO.Path.GetDirectoryName(prjName), "BimEngineData.get");
                using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen("getFileName", 1024 * 1024, MemoryMappedFileAccess.ReadWrite))
                {
                    using (var stream = mmf.CreateViewStream())
                    {
                        IFormatter formatter = new BinaryFormatter();
                        formatter.Serialize(stream, fileName);
                    }
                    FileMutex.ReleaseMutex();
                    CadMutex.WaitOne();
                }

                if (getFileType== "structrue")
                {
                    
                    if (CurrentDocument != null && CurrentDocument.AllBimProjects.Count > 0)
                    {
                        //var prjName = CurrentDocument.AllBimProjects.First().ProjectIdentity.Split('.').First() + "-100%.ifc";
                        var ifcStore = ThBimCutData.GetIfcStore(prjName);
                        var readGeomtry = new IfcStoreReadGeomtry(new XbimMatrix3D());
                        var allGeoModels = readGeomtry.ReadGeomtry(ifcStore, out List<PointNormal> allGeoPointNormals);
                        ThBimCutData.Run(ifcStore, allGeoModels, allGeoPointNormals);
                    }
                }
                else
                {
                    if (CurrentDocument != null && CurrentDocument.AllBimProjects.Count > 0)
                    {
                        var ifcStore = ThBimCutData.GetIfcStore(prjName);
                        var pcPrjName = Path.Combine(System.IO.Path.GetDirectoryName(CurrentDocument.AllBimProjects.First().ProjectIdentity), "【PC】1114-同润二期1#楼tekla不带钢筋信息.ifc");
                        var ifcStorePC = ThBimCutData.GetIfcStore(pcPrjName);
                        var readGeomtry = new IfcStoreReadGeomtry(new XbimMatrix3D());
                        var allGeoModels = readGeomtry.ReadGeomtry(ifcStore, out List<PointNormal> allGeoPointNormals);
                        var readGeomtryPC = new IfcStoreReadGeomtry(new XbimMatrix3D());
                        var allGeoModelsPC = readGeomtryPC.ReadGeomtry(ifcStorePC, out List<PointNormal> allGeoPointNormalsPC);
                        ThBimCutData.Run(ifcStore, allGeoModels, allGeoPointNormals, ifcStorePC, allGeoModelsPC, allGeoPointNormalsPC);
                    }
                }
                

                ViewerMutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                CadMutex?.Dispose();
                ViewerMutex?.Dispose();
                FileMutex?.Dispose();
                FlagMutex?.Dispose();
                ViewerMutex2?.Dispose();
            }
        }


        #region 接收数据并解析数据渲染
        private void Background_DoWork(object sender, DoWorkEventArgs e)
        {
            thMessage = null;
            if (null == pipeServer)
                pipeServer = new NamedPipeServerStream("THCAD2P3DPIPE", PipeDirection.In);
            pipeServer.WaitForConnection();
            try
            {
                thMessage = new ProtobufMessage();
                byte[] PipeData = ReadPipeData(pipeServer);
                Google.Protobuf.MessageExtensions.MergeFrom(thMessage, PipeData);
            }
            catch (IOException ioEx)
            {
                thMessage = null;
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            pipeServer.Dispose();
        }

        private byte[] ReadPipeData(NamedPipeServerStream stream)
        {
            List<byte> result = new List<byte>();
            while (true)
            {
                byte[] bytes = new byte[256];
                var length = stream.Read(bytes, 0, bytes.Length);
                if (length == 256)
                    result.AddRange(bytes);
                else
                {
                    result.AddRange(bytes.Take(length));
                    break;
                }
            }
            return result.ToArray();
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != thMessage)
            {
                var message = thMessage;
                thMessage = null;
                pipeServer = null;
                backgroundWorker.RunWorkerAsync();
                var majer = message.Header.Major;//专业
                var major = EnumUtil.GetEnumItemByDescription<EMajor>(majer);
                ProjectDBHelper projectDB = new ProjectDBHelper();
                foreach (var project in message.CadProjects)
                {
                    var ProjectId = project.ProjectId;//项目信息
                    var ProjectChildId = project.ProjectSubId;//子项信息
                    var BindingName = project.BindingName;//楼栋名称
                    var ProjectPath = project.ProjectPath;//完整路径
                    //获取项目
                    var prj = projectDB.GetProjectAndSubPrj(ProjectId, ProjectChildId);
                    if (null == prj)
                        continue;
                    var fileName = Path.GetFileNameWithoutExtension(ProjectPath);
                    var ifcPath =Path.Combine(ProjectCommon.GetPrjectSubDir(prj,majer,"CAD"),string.Format("{0}_{1}.ifc",fileName, BindingName));
                    //打印CAD管道数据
                    var Model = ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Factory.CreateAndInitModel("ThCAD2IFCProject", project.Root.GlobalId);
                    if (Model != null)
                    {
                        ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Builder.BuildIfcModel(Model, project);
                        ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Builder.SaveIfcModel(Model, ifcPath);
                        Model.Dispose();
                    }
                    CurrentDocument.AddProject(project, new ProjectParameter
                    {
                        ProjectId = project.Root.GlobalId,
                        Source = EApplcationName.CAD,
                        Major = EMajor.Architecture,
                    });
                }
            }
        }

        private void SU_Background_DoWork(object sender, DoWorkEventArgs e)
        {
            if (null == SU_pipeServer)
                SU_pipeServer = new NamedPipeServerStream("THSU2P3DIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            SU_pipeServer.WaitForConnection();
            try
            {
                suMessage = new ProtobufMessage();
                byte[] PipeData = ReadPipeData(SU_pipeServer);
                Google.Protobuf.MessageExtensions.MergeFrom(suMessage, PipeData);
            }
            catch (IOException ioEx)
            {
                suMessage = null;
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            SU_pipeServer.Dispose();
        }

        private void SU_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != suMessage)
            {
                var message = suMessage;
                suMessage = null;
                SU_pipeServer = null;
                SU_backgroundWorker.RunWorkerAsync();
                if (documentModelCache == null || !documentModelCache.ContainsKey(CurrentDocument.DocumentId)) 
                {
                    Log.Info("SU端数据和当前打开的项目不是同一项目的同一楼栋，数据已丢弃");
                    return;
                }
                var cacheModel = documentModelCache[CurrentDocument.DocumentId];
                cacheModel.UpdateCacheFile();
                var mainDir = "";
                if (cacheModel.MainModel != null) 
                {
                    mainDir = Path.GetDirectoryName(cacheModel.MainModel.LoaclPath);
                    mainDir = Path.GetDirectoryName(mainDir);
                }
                var majer = message.Header.Major;//专业
                foreach (var project in message.SuProjects)
                {
                    var ProjectPath = project.ProjectPath;//完整路径
                    //判断是否是当前打开的项目
                    if (string.IsNullOrEmpty(ProjectPath)) 
                    {
                        Log.Info("SU端数据和当前打开的项目不是同一项目的同一楼栋，数据已丢弃");
                        continue;
                    }
                    var dir = Path.GetDirectoryName(ProjectPath);
                    dir = Path.GetDirectoryName(dir);
                    if (!Directory.Equals(mainDir, dir)) 
                    {
                        Log.Info("SU端数据和当前打开的项目不是同一项目的同一楼栋，数据已丢弃");
                        continue;
                    }
                    var fileInfo = cacheModel.GetProjectFileInfo(ProjectPath);
                    if (null == fileInfo)
                        continue;
                    //判断是否有外链记录，如果没有这加入
                    bool haveLink = false;
                    foreach(var item in cacheModel.DocExternalLink.LinkModels) 
                    {
                        if (item.Project.LoaclPath == ProjectPath) 
                        {
                            haveLink = true;
                            break;
                        }
                    }
                    var ifcPath = Path.GetDirectoryName(fileInfo.LoaclPath);
                    var fileName = Path.GetFileNameWithoutExtension(fileInfo.LoaclPath);
                    ifcPath = Path.Combine(ifcPath, string.Format("{0}.ifc", fileName));
                    if (!haveLink)
                    {
                        fileInfo.LinkFilePath = ifcPath;
                        cacheModel.DocExternalLink.LinkModels.Add(new LinkModel()
                        {
                            LinkId = Guid.NewGuid().ToString(),
                            LinkState = "已链接",
                            Project = fileInfo,
                            MoveMatrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero),
                            RotainAngle = 0,
                        });
                        cacheModel.DocExternalLink.SaveToFile();
                    }
                    else 
                    {
                        //判断是否要将已经载入的IFC移除，再次载入SU Push Data
                        THBimProject rmPrj = null;
                        foreach (var prj in CurrentDocument.AllBimProjects) 
                        {
                            if (prj.ApplcationName != EApplcationName.SU && prj.Major == fileInfo.Major)
                                continue;
                            var isIfc = prj.SourceProject is IfcStore;
                            if (!isIfc)
                                continue;
                            if (prj.ProjectIdentity == fileInfo.LinkFilePath)
                                rmPrj = prj;
                        }
                        if(null != rmPrj)
                            CurrentDocument.DeleteProject(rmPrj.ProjectIdentity);
                    }
                    //打印SU过来的管道数据
                    
                    var Model = ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Factory.CreateAndInitModel("ThSU2IFCProject", project.Root.GlobalId);
                    if (Model != null)
                    {
                        ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Builder.BuildIfcModel(Model, project);
                        ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Builder.SaveIfcModel(Model, ifcPath);
                        Model.Dispose();
                    }
                    CurrentDocument.AddProject(project, new ProjectParameter
                    {
                        ProjectId = project.Root.GlobalId,
                        Source = EApplcationName.SU,
                        Major = fileInfo.Major,
                    });
                }
            }
        }

        private void ifc_Background_DoWork(object sender, DoWorkEventArgs e)
        {
            if (ifc_pipeServer == null)
                ifc_pipeServer = new NamedPipeServerStream("THIFCSTREAM2P3DIPE", PipeDirection.In);
            ifc_pipeServer.WaitForConnection();
            try
            {
                var buffer = ReadPipeData(ifc_pipeServer);
                var stream = new MemoryStream(buffer);
                streamParameter = new StreamParameter(stream, 
                    Xbim.IO.IfcStorageType.Ifc, 
                    Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3, 
                    Xbim.Ifc.XbimModelType.MemoryModel);
            }
            catch (IOException ioEx)
            {
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            ifc_pipeServer.Dispose();
        }

        private void ifc_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ifc_backgroundWorker.RunWorkerAsync();
            LoadStreamToCurrentDocument(streamParameter);
        }

        private void file_Background_DoWork(object sender, DoWorkEventArgs e)
        {
            ifc_ProjectPath = string.Empty;
            if (file_pipeServer == null)
                file_pipeServer = new NamedPipeServerStream("THIFCFILE2P3DPIE", PipeDirection.In);
            file_pipeServer.WaitForConnection();
            try
            {
                var PipeData = ReadPipeData(file_pipeServer);
                ifc_ProjectPath = Encoding.UTF8.GetString(PipeData, 0, PipeData.Length);
            }
            catch (IOException ioEx)
            {
                ifc_ProjectPath = string.Empty;
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            file_pipeServer.Dispose();
        }

        private void file_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ifc_ProjectPath))
            {
                #region 临时代码(结构ydb和su合模代码)
                IfcStore ydbIfc = null;
                foreach (var item in CurrentDocument.AllBimProjects)
                {
                    if (item.SourceProject == null)
                        continue;
                    var ifcS = item.SourceProject as IfcStore;
                    if (null == ifcS)
                        continue;
                    if (ifcS.FileName.ToLower().EndsWith("ifc") && ifcS.FileName != ifc_ProjectPath)
                    {
                        ydbIfc = ifcS;
                        break;
                    }
                }
                if (null != ydbIfc)
                {
                    var mergeService = new THModelMergeService();
                    var mergeIfc = mergeService.ModelMerge(ydbIfc.FileName, ifc_ProjectPath);
                    var fileName = Path.GetFileNameWithoutExtension(ydbIfc.FileName);
                    var dirName = Path.GetDirectoryName(ydbIfc.FileName);
                    fileName = string.Format("{0}-100%.ifc", fileName);
                    var newName = Path.Combine(dirName, fileName);
                    mergeIfc.SaveAs(newName);
                }
                #endregion
                file_pipeServer = null;
                file_backgroundWorker.RunWorkerAsync();
                LoadFileToCurrentDocument(new ProjectParameter
                {
                    OpenFilePath = ifc_ProjectPath,
                    ProjectId = ifc_ProjectPath,
                    Source = EApplcationName.SU,
                    Major = EMajor.Structure,
                });
            }
        }
        #endregion
    }
}
