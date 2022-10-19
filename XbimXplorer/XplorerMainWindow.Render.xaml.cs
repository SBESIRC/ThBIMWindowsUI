using System;
using System.Collections.Generic;
using System.Windows;
using THBimEngine.Domain;
using THBimEngine.Domain.Grid;
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
        public void ShowGridByIds(List<string> gridEntityIds)
        {
            if (gridEntityIds == null)
                return;
            ExampleScene.ifcre_set_sleep_time(100);
            var storeToEngineFile = new IfcStoreToEngineFile();
            List<GridLine> showGridLines = new List<GridLine>();
            List<GridCircle> showGridCircles = new List<GridCircle>();
            List<GridText> showGridTexts = new List<GridText>();
            if (gridEntityIds.Count > 0) 
            {
                foreach (var item in CurrentScene.AllGridLines)
                {
                    if (gridEntityIds.Contains(item.Uid))
                        showGridLines.Add(item);
                }
                foreach (var item in CurrentScene.AllGridCircles)
                {
                    if (gridEntityIds.Contains(item.Uid))
                        showGridCircles.Add(item);
                }
                foreach (var item in CurrentScene.AllGridTexts)
                {
                    if (gridEntityIds.Contains(item.Uid))
                        showGridTexts.Add(item);
                }
            }
            storeToEngineFile.PushGridDataToEngine(showGridLines, showGridCircles, showGridTexts);
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
            DateTime start = DateTime.Now;
            currentDocument.UpdateCatchStoreyRelation();
            currentDocument.ReadGeometryMesh();
            CurrentScene = new THBimScene(currentDocument.DocumentId);
            foreach (var item in CurrentDocument.AllGeoModels) 
            {
                CurrentScene.AllGeoModels.Add(item);
            }
            foreach (var item in CurrentDocument.AllGeoPointNormals) 
            {
                CurrentScene.AllGeoPointNormals.Add(item);
            }
            var startGIndex = CurrentScene.AllGeoModels.Count + 1;
            foreach (var prj  in CurrentDocument.AllBimProjects)
            {
                foreach(var item in prj.PrjAllEntitys.Values)
                {
                    if(item.GetType().Name== "GridLine")
                    {
                        CurrentScene.AllGridLines.Add(item as GridLine);
                        CurrentDocument.MeshEntiyRelationIndexs.Add(startGIndex, new MeshEntityIdentifier(startGIndex, prj.ProjectIdentity, item.Uid));
                        startGIndex += 1;
                    }
                    if (item.GetType().Name == "GridCircle")
                    {
                        CurrentScene.AllGridCircles.Add(item as GridCircle);
                        CurrentDocument.MeshEntiyRelationIndexs.Add(startGIndex, new MeshEntityIdentifier(startGIndex, prj.ProjectIdentity, item.Uid));
                        startGIndex += 1;
                    }
                    if (item.GetType().Name == "GridText")
                    {
                        CurrentScene.AllGridTexts.Add(item as GridText);
                        CurrentDocument.MeshEntiyRelationIndexs.Add(startGIndex, new MeshEntityIdentifier(startGIndex, prj.ProjectIdentity, item.Uid));
                        startGIndex += 1;
                    }
                }
            }
            var storeToEngineFile = new IfcStoreToEngineFile();
            storeToEngineFile.WriteMidDataMultithreading(CurrentScene.AllGeoModels,CurrentScene.AllGeoPointNormals);
            storeToEngineFile.PushGridDataToEngine(CurrentScene.AllGridLines, CurrentScene.AllGridCircles, CurrentScene.AllGridTexts);
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
