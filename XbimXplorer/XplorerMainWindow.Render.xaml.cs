using System;
using System.Collections.Generic;
using System.Windows;
using THBimEngine.Domain;
using THBimEngine.Presention;
using Xbim.Common.Geometry;
using XbimXplorer.ThBIMEngine;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        public void ShowEntityByIds(List<int> showEntityIds)
        {
            if (showEntityIds == null)
                return;
            ExampleScene.ifcre_set_config("to_show_states", "0");
            ExampleScene.ifcre_set_comp_ids(-1);
            ExampleScene.ifcre_set_sleep_time(100);
            foreach (var id in showEntityIds)
            {
                ExampleScene.ifcre_set_comp_ids(id);
            }
            ExampleScene.ifcre_set_sleep_time(10);
        }

        public void RenderScene()
        {
            if (CurrentApplicationIsDisposed())
                return;
            CurrentDocumentToScene();
            DocumentChanged.Invoke(currentDocument, null);
            projectMatrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
            DateTime startTime = DateTime.Now;
            ProgressBar.Value = 0;
            StatusMsg.Text = "";
            startTime = DateTime.Now;
            var formHost = winFormHost;
            var childConrol = formHost.Child as GLControl;
            childConrol.EnableNativeInput();
            childConrol.MakeCurrent();
            ExampleScene.Init(childConrol.Handle, childConrol.Width, childConrol.Height, "");
            DateTime endTime = DateTime.Now;
            var totalTime = (endTime - startTime).TotalSeconds;
            Log.Info(string.Format("渲染前准备工作完成，耗时：{0}s", totalTime));
            _dispatcherTimer.Start();
            ExampleScene.Render();
        }

        public void SelectEntityIds(List<int> selectIds)
        {
            throw new NotImplementedException();
        }

        public int GetSelectId()
        {
            throw new NotImplementedException();
        }

        public void ZoomEntitys(List<int> roomIds)
        {
            if (null == roomIds || roomIds.Count < 1)
            {
                ExampleScene.ifcre_home();
            }
            else
            {

            }
        }
        private void CurrentDocumentToScene() 
        {
            CurrentScene = null;
            if (CurrentDocument == null)
                return;
            CurrentScene = new THBimScene(currentDocument.DocumentId);
            foreach (var item in CurrentDocument.AllGeoModels) 
            {
                CurrentScene.AllGeoModels.Add(item);
            }
            foreach (var item in CurrentDocument.AllGeoPointNormals) 
            {
                CurrentScene.AllGeoPointNormals.Add(item);
            }
            DateTime start = DateTime.Now;
            var storeToEngineFile = new IfcStoreToEngineFile();
            storeToEngineFile.WriteMidDataMultithreading(CurrentScene.AllGeoModels, CurrentScene.AllGeoPointNormals);
            DateTime end = DateTime.Now;
            var totalTime = (end - start).TotalSeconds;
            Log.Info(string.Format("数据发送引擎完成，耗时：{0}s", totalTime));
        }
        private bool CurrentApplicationIsDisposed() 
        {
            return Application.Current == null;
        }
    }
}
