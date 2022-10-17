using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using THBimEngine.Application;
using THBimEngine.Domain;
using THBimEngine.Geometry.ProjectFactory;
using Xbim.Common.Geometry;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace XbimXplorer.ThBIMEngine
{
    class ThBimDataController
    {
        ConvertFactoryBase convertFactory;
        private IEngineApplication engineApp;
        private THDocument currentDocument;
        public bool HaveChange { get; protected set; }
        public ThBimDataController(IEngineApplication engineApplication)
        {
            engineApp = engineApplication;
            HaveChange = false;
        }

        public void AddProject(ThTCHProjectData project, XbimMatrix3D matrix3D)
        {
            CalcCurrentScene();
            convertFactory = new THProjectDataConvertFactory(Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3);
            bool isAdd = IsAddProject(project.Root.GlobalId);
            if (isAdd)
            {
                var convertResult = convertFactory.ProjectConvert(project, true);
                convertResult.BimProject.Matrix3D = matrix3D;
                if (null != convertResult)
                {
                    convertResult.BimProject.HaveChange = true;
                    currentDocument.AddProject(convertResult.BimProject);
                    currentDocument.UpdateCatchStoreyRelation();
                    HaveChange = true;
                    currentDocument.ReadGeometryMesh();
                    SetNewSceneToSystem();
                    RenderCurrentDocument();
                }
            }
            else
            {
                var convertResult = convertFactory.ProjectConvert(project, false);
                convertResult.BimProject.Matrix3D = matrix3D;
                UpdateProject(convertResult);
            }
            
        }
        public void AddProject(ThSUProjectData project, XbimMatrix3D matrix3D)
        {
            CalcCurrentScene();
            convertFactory = new THSUProjectConvertFactory(Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3);
            bool isAdd = IsAddProject(project.Root.GlobalId);
            if (!isAdd)
            {
                //这里增量跟新没有做，先删除原来的数据，再增加现在的数据
                currentDocument.DeleteProjectData(project.Root.GlobalId);
            }
            var convertResult = convertFactory.ProjectConvert(project, true);
            var bimProject = convertResult.BimProject;
            bimProject.ProjectIdentity = project.Root.GlobalId;
            bimProject.SourceProject = project;
            //bimProject.NeedCreateMesh = false;
            bimProject.HaveChange = true;
            HaveChange = true;
            //var allGeoPointNormals = new List<PointNormal>();
            //var readGeomtry = new SUProjectReadGeomtry();
            //var allGeoModels = readGeomtry.ReadGeomtry(project, matrix3D, out allGeoPointNormals);
            //bimProject.AddGeoMeshModels(allGeoModels, allGeoPointNormals);
            currentDocument.AddProject(bimProject);
            currentDocument.ReadGeometryMesh();
            SetNewSceneToSystem();
            RenderCurrentDocument();
        }

        /// <summary>
        /// 这里目前只处理Mesh后的IfcStore
        /// </summary>
        /// <param name="ifcStore"></param>
        public void AddProject(IfcStore ifcStore, XbimMatrix3D matrix3D) 
        {
            CalcCurrentScene();
            convertFactory = new THIfcStoreMeshConvertFactory(ifcStore.IfcSchemaVersion);
            var isAdd = IsAddProject(ifcStore.FileName);
            if (!isAdd) 
            {
                //这里增量跟新没有做，先删除原来的数据，再增加现在的数据
                currentDocument.DeleteProjectData(ifcStore.FileName);
            }
            var prjName = Path.GetFileNameWithoutExtension(ifcStore.FileName);
            var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            var bimProject = new THBimProject(0, prjName, "", ifcProject.GlobalId);
            bimProject.SourceName = "IFC";
            bimProject.ProjectIdentity = ifcStore.FileName;
            bimProject.SourceProject = ifcStore;
            bimProject.NeedCreateMesh = false;
            var allGeoPointNormals = new List<PointNormal>();
            var readGeomtry = new IfcStoreReadGeomtry(matrix3D);
            var allGeoModels = readGeomtry.ReadGeomtry(ifcStore, out allGeoPointNormals);
            bimProject.AddGeoMeshModels(allGeoModels, allGeoPointNormals);
            currentDocument.AddProject(bimProject);
            currentDocument.ReadGeometryMesh();
            SetNewSceneToSystem();
            RenderCurrentDocument();
        }


        private void CalcCurrentScene()
        {
            if (engineApp.CurrentDocument == null)
            {
                engineApp.CurrentDocument = engineApp.AllDocuments.FirstOrDefault();
            }
            if (engineApp.CurrentDocument == null)
            {
                currentDocument = new THDocument(System.Guid.NewGuid().ToString(), "测试项目");
                currentDocument.UnShowEntityTypes.Add(typeof(THBimOpening).Name.ToString());
                currentDocument.UnShowEntityTypes.Add("open");
            }
        }
        private void SetNewSceneToSystem()
        {
            if (!engineApp.AllDocuments.Contains(currentDocument))
                engineApp.AllDocuments.Add(currentDocument);
            engineApp.CurrentDocument = currentDocument;
        }
        public THBimScene AddTHScene()
        {
            return null;
        }
        public Dictionary<string, object> GetSelectEntityProperties(int index) 
        {
            return currentDocument.SelectEntityProperties(index);
        }
        public void DeleteProject(List<string> delPrjIds)
        {
            foreach (var item in delPrjIds) 
            {
                currentDocument.DeleteProjectData(item);
            }
            RenderCurrentDocument();
        }
        private void UpdateProject(ConvertResult projectResult)
        {
            var prjId = projectResult.BimProject.Uid;
            var newBimProject = projectResult.BimProject;
            var newEntitys = projectResult.ProjectEntitys;

            HaveChange = false;
            foreach (var project in currentDocument.AllBimProjects)
            {
                if (project.ProjectIdentity != projectResult.BimProject.ProjectIdentity)
                    continue;
                var buildings = project.ProjectSite.SiteBuildings.Values;
                var updateMeshIds = new List<string>();
                foreach (var building in buildings)
                {
                    var buildingId = building.Uid;
                    var id2BuildingDic = newBimProject.ProjectSite.SiteBuildings;
                    var newBuilding = id2BuildingDic[buildingId];
                    #region 楼层变化的相关信息
                    /*
                     楼层变化的信息比较复杂
                    1、楼层增加、删除 1，2，3  =》1，2，3，4
                    2、标准层的增加、删除 1，2，3-10 =》 1，2，3-14  =》1，2，3-7，8-10 =》1，2，3-5，6
                    3、标准层的变化 1，2，3-10 =》1，2，3，4-12
                    可能出现组合情况
                     */
                    #endregion
                    var delFloorIds = new List<string>();
                    //step1 处理删除掉的数据(1、删除的楼层；2、删除的构件；3、变换楼层的构件;4、楼层的变化)
                    foreach (var storey in building.BuildingStoreys.Values.ToList())
                    {
                        var haveEntity = string.IsNullOrEmpty(storey.MemoryStoreyId);
                        var newRemovedUids = new List<string>();
                        if (newBuilding.BuildingStoreys.ContainsKey(storey.Uid))
                        {
                            if (!haveEntity)
                                continue;
                            var newStorey = newBuilding.BuildingStoreys[storey.Uid];
                            if (!string.IsNullOrEmpty(newStorey.MemoryStoreyId))
                            {
                                //楼层变为标准层非首层数据
                                newRemovedUids.AddRange(storey.FloorEntityRelations.Select(c => c.Key).ToList());
                            }
                            else
                            {
                                newRemovedUids = storey.GetRemovedComponentUids(newStorey);
                            }
                        }
                        else
                        {
                            //楼层已经删除
                            delFloorIds.Add(storey.Uid);
                            if (!haveEntity)
                                continue;
                            newRemovedUids.AddRange(storey.FloorEntityRelations.Select(c => c.Key).ToList());
                        }
                        if (newRemovedUids.Count > 0)
                        {
                            HaveChange = true;
                            RemoveEntitys(building, newRemovedUids);
                        }
                    }
                    foreach (var delStorey in delFloorIds)
                    {
                        building.BuildingStoreys.Remove(delStorey);
                    }
                    //step2 处理添加的数据（1、楼层非标或标准层的首层；2、对应楼层增加的Entity）
                    var meshEntityIds = new List<string>();
                    var oldStoreys = building.BuildingStoreys.Values.ToList();
                    var newStoreys = newBuilding.BuildingStoreys.Values.ToList();
                    var oldNoMemoryUids = oldStoreys.Where(c => string.IsNullOrEmpty(c.MemoryStoreyId)).Select(c => c.Uid).ToList();
                    var newNoMemoryUids = newStoreys.Where(c => string.IsNullOrEmpty(c.MemoryStoreyId)).Select(c => c.Uid).ToList();
                    var addNoMemoryUids = newNoMemoryUids.Except(oldNoMemoryUids);
                    foreach (var addId in addNoMemoryUids)
                    {
                        HaveChange = true;
                        var thisStorey = newStoreys.Find(c => c.Uid == addId);
                        var thisStoreyEntityIds = thisStorey.FloorEntityRelations.Select(c => c.Key).ToList();
                        var addEntitys = new Dictionary<string, THBimEntity>();
                        foreach (var id in thisStoreyEntityIds) 
                        {
                            addEntitys.Add(id, newEntitys[id]);
                        }
                        updateMeshIds.AddRange(thisStoreyEntityIds);
                        foreach (var floorEntity in thisStorey.FloorEntityRelations) 
                        {
                            var entity = addEntitys[floorEntity.Key];
                            floorEntity.Value.Id = entity.Id;
                        }
                        if (building.BuildingStoreys.ContainsKey(addId))
                        {
                            //标准层非首层变为非标层或标准层的首层
                            var storey = building.BuildingStoreys[addId];
                            storey.MemoryStoreyId = string.Empty;
                            storey.FloorEntityRelations.Clear();
                            foreach (var floorEntity in thisStorey.FloorEntityRelations)
                            {
                                storey.FloorEntityRelations.Add(floorEntity.Key, floorEntity.Value);
                            }
                            foreach (var floorEntity in addEntitys)
                            {
                                storey.FloorEntitys.Add(floorEntity.Key, floorEntity.Value);
                            }
                        }
                        else
                        {
                            //增加的楼层
                            building.BuildingStoreys.Add(thisStorey.Uid, thisStorey);
                        }
                    }
                    //step3 处理增加的楼层信息（标准层非首层）
                    var oldStoreyIds = building.BuildingStoreys.Keys.ToList();
                    var newStoreyIds = newBuilding.BuildingStoreys.Keys.ToList();
                    var allAddStoreys = newStoreyIds.Except(oldStoreyIds).ToList();
                    foreach (var addId in allAddStoreys) 
                    {
                        if (building.BuildingStoreys.ContainsKey(addId))
                            continue;
                        HaveChange = true;
                        //新增标准层非首层数据
                        var thisStorey = newStoreys.Find(c => c.Uid == addId);
                        building.BuildingStoreys.Add(thisStorey.Uid, thisStorey);
                    }
                    //step4 处理楼层所属标准层变更的楼层
                    foreach (var storeyKeyValue in newBuilding.BuildingStoreys) 
                    {
                        if (string.IsNullOrEmpty(storeyKeyValue.Value.MemoryStoreyId))
                            continue;
                        var oldStorey = building.BuildingStoreys[storeyKeyValue.Key];
                        if (oldStorey.MemoryStoreyId == storeyKeyValue.Value.MemoryStoreyId)
                            continue;
                        HaveChange = true;
                        oldStorey.MemoryStoreyId = storeyKeyValue.Value.MemoryStoreyId;
                        oldStorey.MemoryMatrix3d = storeyKeyValue.Value.MemoryMatrix3d;
                        oldStorey.FloorEntityRelations.Clear();
                        foreach (var floorEntity in storeyKeyValue.Value.FloorEntityRelations)
                        {
                            oldStorey.FloorEntityRelations.Add(floorEntity.Key, floorEntity.Value);
                        }
                    }
                    //step5 处理没有变更非标层或标准层首层的信息
                    var checkStoreyIds = oldNoMemoryUids.Intersect(newNoMemoryUids);
                    foreach (var storeyId in checkStoreyIds) 
                    {
                        var oldStorey = building.BuildingStoreys[storeyId];
                        var newStorey = newBuilding.BuildingStoreys[storeyId];
                        var newAddedUids = oldStorey.GetAddedComponentUids(newStorey);
                        var newUpdatedUids = oldStorey.GetUpdatedComponentUids(newStorey);
                        if (newAddedUids.Count > 0)
                        {
                            HaveChange = true;
                            updateMeshIds.AddRange(newAddedUids);
                            var addEntitys = newEntitys.Where(c => newAddedUids.Any(x => x == c.Key)).ToDictionary(c => c.Key, x => x.Value);
                            AddEntitys(building, addEntitys);
                        }
                        if (newUpdatedUids.Count > 0)
                        {
                            HaveChange = true;
                            updateMeshIds.AddRange(newUpdatedUids);
                            var updateEntitys = newEntitys.Where(c => newUpdatedUids.Any(x => x == c.Key)).ToDictionary(c => c.Key, x => x.Value);
                            UpdateEntitys(updateEntitys);
                        }
                    }
                    //step6 楼层层高标高变更修改
                    var tempIds = oldStoreyIds.Intersect(newStoreyIds);
                    foreach (var storeyId in tempIds) 
                    {
                        var oldStorey = building.BuildingStoreys[storeyId];
                        var newStorey = newBuilding.BuildingStoreys[storeyId];
                        if (Math.Abs(oldStorey.Elevation - newStorey.Elevation) > 1)
                        {
                            HaveChange = true;
                            oldStorey.Elevation = newStorey.Elevation;
                            oldStorey.Matrix3D = newStorey.Matrix3D;
                        }
                        if (Math.Abs(oldStorey.LevelHeight - newStorey.LevelHeight) > 1) 
                        {
                            HaveChange = true;
                            oldStorey.LevelHeight = newStorey.LevelHeight;
                            oldStorey.Matrix3D = newStorey.Matrix3D;
                        }
                    }
                }
                if (HaveChange) 
                {
                    project.HaveChange = true;
                    currentDocument.UpdateCatchStoreyRelation();
                    UpateEntitySolidMesh(updateMeshIds);
                }
            }
            if (HaveChange)
            {
                currentDocument.ReadGeometryMesh();
                SetNewSceneToSystem();
                RenderCurrentDocument(); 
            }
        }
        public void ClearAllProject()
        {
            if(null != currentDocument)
                currentDocument.ClearAllData();
        }
        public void RenderCurrentDocument()
        {
            //engineApp.RenderScene();
        }
        private bool IsAddProject(string prjIdentity) 
        {
            bool isAdd = true;
            foreach (var item in currentDocument.AllBimProjects)
            {
                if (item.ProjectIdentity == prjIdentity)
                {
                    isAdd = false;
                    break;
                }
            }
            return isAdd;
        }
        private void RemoveEntitys(THBimBuilding bimBuilding, List<string> rmEntityIds) 
        {
            var thisStoreys = bimBuilding.BuildingStoreys;
            if (null == rmEntityIds || rmEntityIds.Count < 1)
                return;
            var rmIds = new List<string>();
            foreach (var entityId in rmEntityIds)
            {
                if (!currentDocument.AllEntitys.ContainsKey(entityId))
                    continue;
                var entity = currentDocument.AllEntitys[entityId];
                var pid = entity.ParentUid;
                while (!string.IsNullOrEmpty(pid) && !thisStoreys.ContainsKey(pid))
                {
                    var pEntity = currentDocument.AllEntitys[pid];
                    pid = pEntity.ParentUid;
                }
                rmIds.Add(entityId);
                if (string.IsNullOrEmpty(pid))
                    continue;
                foreach (var storeyKeyValue in thisStoreys)
                {
                    var storey = storeyKeyValue.Value;
                    if (storey.Uid != pid && storey.MemoryStoreyId != pid)
                        continue;
                    var rmRealtion = storey.FloorEntityRelations.Where(c => c.Value.RelationElementUid == entityId).Select(c => c.Key).ToList();
                    foreach (var rmId in rmRealtion)
                    {
                        storey.FloorEntityRelations.Remove(rmId);
                    }
                }
            }
            foreach (var rmId in rmIds) 
            {
                currentDocument.AllEntitys.Remove(rmId);
            }
        }
        private void AddEntitys(THBimBuilding building, Dictionary<string,THBimEntity> addEntitys) 
        {
            var idOffSet = 0;// LastEntityIntId() + 1;
            foreach (var entityKeyValue in addEntitys)
            {
                var entity = entityKeyValue.Value;
                var pid = entity.ParentUid;
                while (!string.IsNullOrEmpty(pid) && !building.BuildingStoreys.ContainsKey(pid))
                {
                    //如果只是增加了门窗，父元素为墙，算更新数据，不算增的数据
                    var pEntity = addEntitys.ContainsKey(pid)? addEntitys[pid]: currentDocument.AllEntitys[pid];
                    pid = pEntity.ParentUid;
                }
                if (string.IsNullOrEmpty(pid) || !building.BuildingStoreys.ContainsKey(pid))
                    continue;
                entityKeyValue.Value.Id = idOffSet;
                foreach (var storeyKeyValue in building.BuildingStoreys)
                {
                    var storey = storeyKeyValue.Value;
                    if (storey.Uid != pid && storey.MemoryStoreyId != pid)
                        continue;
                    if (string.IsNullOrEmpty(storey.MemoryStoreyId)) 
                    {
                        storey.FloorEntitys.Add(entity.Uid, entity);
                    }
                    var uid = storey.Uid == pid ? entity.Uid : string.Empty;
                    var addRelation = new THBimElementRelation(idOffSet, entity.FriendlyTypeName,null,"", uid);
                    addRelation.ParentUid = storey.Uid;
                    addRelation.RelationElementUid = entity.Uid;
                    addRelation.RelationElementId = entity.Id;
                    storey.FloorEntityRelations.Add(addRelation.Uid, addRelation);
                    idOffSet += 1;
                }
            }
        }
        
        private void UpdateEntitys(Dictionary<string, THBimEntity> updateEntitys)
        {
            if (null == updateEntitys || updateEntitys.Count < 1)
                return;
            foreach (var keyValue in updateEntitys) 
            {
                var id = keyValue.Key;
                var entity = keyValue.Value;
                var oldValue = currentDocument.AllEntitys[id];
                oldValue.GeometryParam = entity.GeometryParam;
                oldValue.EntitySolids.Clear();
                oldValue.AllShapeGeometries.Clear();
                oldValue.Openings.Clear();
                foreach (var item in entity.Openings)
                    oldValue.Openings.Add(item);
            }
        }
        private void UpateEntitySolidMesh(List<string> updateEntityIds) 
        {
            if (null == updateEntityIds || updateEntityIds.Count < 1)
                return;
            List<THBimEntity> updateEntitys = new List<THBimEntity>();
            foreach (var id in updateEntityIds) 
            {
                if(!currentDocument.AllEntitys.ContainsKey(id))
                    continue;
                var entity = currentDocument.AllEntitys[id];
                updateEntitys.Add(entity);
                foreach (var item in entity.Openings)
                {
                    updateEntitys.Add(currentDocument.AllEntitys[item.Uid]);
                }
            }
            if (updateEntitys.Count < 1)
                return;
            convertFactory.CreateSolidMesh(updateEntitys);
        }
        
    }
}
