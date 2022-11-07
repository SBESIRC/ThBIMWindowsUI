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

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        private ThTCHProjectData thProject = null;
        private ThSUProjectData suProject = null;
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
                //Mutex CadMutex;
                while (true)
                {
                    var flag = Mutex.TryOpenExisting("cadMutex", out CadMutex);
                    if (flag) break;
                    Thread.Sleep(100);
                }
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
                if (CurrentDocument != null && CurrentDocument.AllBimProjects.Count > 0)
                {
                    //var prjName = CurrentDocument.AllBimProjects.First().ProjectIdentity.Split('.').First() + "-100%.ifc";
                    var ifcStore = ThBimCutData.GetIfcStore(prjName);
                    var readGeomtry = new IfcStoreReadGeomtry(new XbimMatrix3D());
                    var allGeoModels = readGeomtry.ReadGeomtry(ifcStore, out List<PointNormal> allGeoPointNormals);
                    ThBimCutData.Run(ifcStore, allGeoModels, allGeoPointNormals);
                }

                ViewerMutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                ;
            }
            finally
            {
                ViewerMutex?.Dispose();
                CadMutex?.Dispose();
                FileMutex?.Dispose();
            }
        }
        #region 接收数据并解析数据渲染
        private void Background_DoWork(object sender, DoWorkEventArgs e)
        {
            thProject = null;
            if (null == pipeServer)
                pipeServer = new NamedPipeServerStream("THCAD2P3DPIPE", PipeDirection.In);
            pipeServer.WaitForConnection();
            try
            {
                thProject = new ThTCHProjectData();
                byte[] PipeData = ReadPipeData(pipeServer);
                if (PipeData.VerifyPipeData())
                {
                    Google.Protobuf.MessageExtensions.MergeFrom(thProject, PipeData.Skip(10).ToArray());
                }
                else
                {
                    throw new Exception("无法识别的CAD-Push数据!");
                }
            }
            catch (IOException ioEx)
            {
                thProject = null;
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
            if (null != thProject)
            {
                var project = thProject;
                thProject = null;
                pipeServer = null;
                backgroundWorker.RunWorkerAsync();
                CurrentDocument.AddProject(project, new ProjectParameter
                {
                    ProjectId = project.Root.GlobalId,
                    Matrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero),
                    Source = EApplcationName.CAD,
                    Major = EMajor.Architecture,
                }); 
                
            }
        }

        private void SU_Background_DoWork(object sender, DoWorkEventArgs e)
        {
            if (null == SU_pipeServer)
                SU_pipeServer = new NamedPipeServerStream("THSU2P3DIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            SU_pipeServer.WaitForConnection();
            try
            {
                suProject = new ThSUProjectData();
                byte[] PipeData = ReadPipeData(SU_pipeServer);
                if (PipeData.VerifyPipeData())
                {
                    Google.Protobuf.MessageExtensions.MergeFrom(suProject, PipeData.Skip(10).ToArray());
                }
                else
                {
                    throw new Exception("无法识别的SU-Push数据!");
                }
            }
            catch (IOException ioEx)
            {
                suProject = null;
                Log.Error(string.Format("ERROR: {0}", ioEx.Message));
            }
            SU_pipeServer.Dispose();
        }

        private void SU_BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != suProject)
            {
                var project = suProject;
                ////打印SU过来的管道数据
                //var Model = ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Factory.CreateAndInitModel("ThSU2IFCProject", project.Root.GlobalId);
                //if (Model != null)
                //{
                //    ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Builder.BuildIfcModel(Model, project);
                //    ThBIMServer.Ifc2x3.ThProtoBuf2IFC2x3Builder.SaveIfcModel(Model, @"D:\test.ifc");
                //    Model.Dispose();
                //}
                suProject = null;
                SU_pipeServer = null;
                SU_backgroundWorker.RunWorkerAsync();
                CurrentDocument.AddProject(project, new ProjectParameter
                {
                    ProjectId = project.Root.GlobalId,
                    Matrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero),
                    Source = EApplcationName.SU,
                    Major = EMajor.Architecture,
                });
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
                    Matrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero),
                    Source = EApplcationName.SU,
                    Major = EMajor.Architecture,
                });
            }
        }
        #endregion
    }
}
