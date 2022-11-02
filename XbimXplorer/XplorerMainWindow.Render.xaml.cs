using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using THBimEngine.Application;
using THBimEngine.Domain;
using THBimEngine.Domain.Grid;
using THBimEngine.Presention;
using Xbim.Common.Geometry;
using XbimXplorer.ThBIMEngine;

namespace XbimXplorer
{
    public partial class XplorerMainWindow
    {
        bool isFirstRender = true;
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
            var dataToEngine = new DataToEngine();
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
            dataToEngine.PushGridDataToEngine(showGridLines, showGridCircles, showGridTexts);
            ExampleScene.ifcre_set_sleep_time(10);
        }
        public void RenderScene()
        {
            if (CurrentApplicationIsDisposed())
                return;
            CurrentDocumentToScene();
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
            ProgressChanged(this, new ProgressChangedEventArgs(0, ""));
            Thread thread = new Thread(new ThreadStart(Func));
            thread.Start();
            ExampleScene.Render();
            
        }

        static Mutex CadMutex = null;
        static Mutex ViewerMutex = null;
        static void InitMutex()
        {
            var viewerMutexName = "viewerMutex";
            try
            {
                ViewerMutex = new Mutex(true, viewerMutexName, out bool viewerMutexCreated);
            }
            catch
            {

                ViewerMutex = Mutex.OpenExisting(viewerMutexName, System.Security.AccessControl.MutexRights.FullControl);
                ViewerMutex.Dispose();

                ViewerMutex = new Mutex(true, viewerMutexName, out bool viewerMutexCreated);
            }
        }
        public void Func()
        {
            InitMutex();
            try
            {
                Mutex CadMutex;
                while (true)
                {
                    if (CurrentDocument == null || CurrentDocument.AllBimProjects.Count < 1)
                        return;
                    var flag = Mutex.TryOpenExisting("cutdata", out CadMutex);
                    if (flag) break;
                    Thread.Sleep(100);
                }

                CadMutex.WaitOne();


                var prjName = CurrentDocument.AllBimProjects.First().ProjectIdentity.Split('.').First() + "-100%.ifc";
                var ifcStore = ThBimCutData.GetIfcStore(prjName);
                var readGeomtry = new IfcStoreReadGeomtry(new XbimMatrix3D());
                var allGeoModels = readGeomtry.ReadGeomtry(ifcStore, out List<PointNormal> allGeoPointNormals);
                ThBimCutData.Run(ifcStore, allGeoModels, allGeoPointNormals);

                //CurrentDocument.DocumentChanged -= XplorerMainWindow_DocumentChanged;
                //CurrentDocument.ClearAllData();
                //CurrentDocument.DocumentChanged += XplorerMainWindow_DocumentChanged;
                //LoadFileToCurrentDocument(prjName, null);
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
            }

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
            CurrentScene = new THBimScene(CurrentDocument.DocumentId);
            foreach (var item in CurrentDocument.AllGeoModels) 
            {
                CurrentScene.AllGeoModels.Add(item);
            }
            foreach (var item in CurrentDocument.AllGeoPointNormals) 
            {
                CurrentScene.AllGeoPointNormals.Add(item);
            }
            foreach (var prj  in CurrentDocument.AllBimProjects)
            {
                foreach(var item in prj.PrjAllEntitys.Values)
                {
                    if(item.GetType().Name== "GridLine")
                    {
                        CurrentScene.AllGridLines.Add(item as GridLine);
                    }
                    if (item.GetType().Name == "GridCircle")
                    {
                        CurrentScene.AllGridCircles.Add(item as GridCircle);
                    }
                    if (item.GetType().Name == "GridText")
                    {
                        CurrentScene.AllGridTexts.Add(item as GridText);
                    }
                }
            }
            var dataToEngine = new DataToEngine();
            dataToEngine.WriteMidDataMultithreading(CurrentScene.AllGeoModels,CurrentScene.AllGeoPointNormals);
            dataToEngine.PushGridDataToEngine(CurrentScene.AllGridLines, CurrentScene.AllGridCircles, CurrentScene.AllGridTexts);
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
