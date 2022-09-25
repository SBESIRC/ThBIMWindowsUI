using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using THBimEngine.Domain;
using THBimEngine.Presention;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        private ThTCHProjectData thProject = null;
        private ThSUProjectData suProject = null;
        NamedPipeServerStream pipeServer = null;
        NamedPipeServerStream SU_pipeServer = null;
        BackgroundWorker backgroundWorker = null;
        BackgroundWorker SU_backgroundWorker = null;
        private void InitPipeService() 
        {
            pipeServer = new NamedPipeServerStream("THCAD2P3DPIPE", PipeDirection.In);
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += Background_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync();

            SU_pipeServer = new NamedPipeServerStream("THSU2P3DPIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
            SU_backgroundWorker = new BackgroundWorker();
            SU_backgroundWorker.DoWork += SU_Background_DoWork;
            SU_backgroundWorker.RunWorkerCompleted += SU_BackgroundWorker_RunWorkerCompleted;
            SU_backgroundWorker.RunWorkerAsync();
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
            List<byte> _current = new List<byte>();
            while (true)
            {
                var i = stream.ReadByte();
                if (i == -1)
                {
                    return _current.ToArray();
                }
                _current.Add(Convert.ToByte(i));
            }
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (null != thProject)
            {
                ExampleScene.ifcre_set_sleep_time(1000);
                DateTime startTime = DateTime.Now;
                bimDataController.AddProject(thProject, projectMatrix3D);
                thProject = null;
                pipeServer = null;
                backgroundWorker.RunWorkerAsync();
                DateTime endTime = DateTime.Now;
                var totalTime = (endTime - startTime).TotalSeconds;
                Log.Info(string.Format("数据解析完成，耗时：{0}s", totalTime));
                RenderScene();
            }
        }

        private void SU_Background_DoWork(object sender, DoWorkEventArgs e)
        {
            if (null == SU_pipeServer)
                SU_pipeServer = new NamedPipeServerStream("THSU2P3DPIPE", PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
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
                ExampleScene.ifcre_set_sleep_time(1000);
                DateTime startTime = DateTime.Now;
                bimDataController.AddProject(suProject, projectMatrix3D);
                suProject = null;
                SU_pipeServer = null;
                SU_backgroundWorker.RunWorkerAsync();
                DateTime endTime = DateTime.Now;
                var totalTime = (endTime - startTime).TotalSeconds;
                Log.Info(string.Format("数据解析完成，耗时：{0}s", totalTime));
                RenderScene();
            }
        }
        #endregion
    }
}
