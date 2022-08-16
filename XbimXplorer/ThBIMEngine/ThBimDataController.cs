using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Model;
using THBimEngine.Geometry;
using THBimEngine.Geometry.ProjectFactory;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;

namespace XbimXplorer.ThBIMEngine
{
    class ThBimDataController
    {
        private Dictionary<string, THBimEntity> _allEntitys { get; }
        private Dictionary<string, THBimStorey> _allStoreys { get; }
        private List<THBimProject> _allBimProject { get; }
        public List<string> UnShowEntityTypes { get; }
        ConvertFactoryBase convertFactory;
        public ThBimDataController()
        {
            _allStoreys = new Dictionary<string, THBimStorey>();
            _allBimProject = new List<THBimProject>();
            _allEntitys = new Dictionary<string, THBimEntity>();
            UnShowEntityTypes = new List<string>();
            UnShowEntityTypes.Add(typeof(THBimOpening).Name.ToString());
            UnShowEntityTypes.Add("opening");
            UnShowEntityTypes.Add("openingelement");
            UnShowEntityTypes.Add("open");
        }
        public void AddProject(ThTCHProject project)
        {
            convertFactory = new THProjectConvertFactory(Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3);
            bool isAdd = IsAddProject(project.Uuid);
            if (isAdd)
            {
                //_allProjects.Add(project);
                var convertResult = convertFactory.ProjectConvert(project,true);
                if (null != convertResult) 
                {
                    _allBimProject.Add(convertResult.BimProject);
                    AddProjectEntitys(convertResult.ProjectEntitys);
                    UpdateCatchStorey();
                    WriteToMidDataByFloor();
                }
            }
            else
            {
                var convertResult = convertFactory.ProjectConvert(project, false);
                UpdateProject(convertResult);
            }
        }
        /// <summary>
        /// 这里目前只处理Mesh后的IfcStore
        /// </summary>
        /// <param name="ifcStore"></param>
        public void AddProject(IfcStore ifcStore) 
        {
            convertFactory = new THIfcStoreMeshConvertFactory(ifcStore.IfcSchemaVersion);
            var isAdd = IsAddProject(ifcStore.FileName);
            if (!isAdd) 
            {
                //这里增量跟新没有做，先删除原来的数据，再增加现在的数据
                DeleteProjectData(ifcStore.FileName);
            }
            //出来的数据是包含Mesh的，后续不需要创建Solid的步骤了
            var convertResult = convertFactory.ProjectConvert(ifcStore,false);
            if (null != convertResult)
            {
                _allBimProject.Add(convertResult.BimProject);
                AddProjectEntitys(convertResult.ProjectEntitys);
            }
            UpdateCatchStorey();
            WriteToMidDataByFloor();
        }

        public void DeleteProject()
        {

        }
        public void UpdateProject(ConvertResult projectResult)
        {
            var prjId = projectResult.BimProject.Uid;
            var newBimProject = projectResult.BimProject;
            var newEntitys = projectResult.ProjectEntitys;
            bool needUpdate = false;
            foreach (var item in _allBimProject)
            {
                if (item.ProjectIdentity != projectResult.BimProject.ProjectIdentity)
                    continue;
                var buildings = item.ProjectSite.SiteBuildings.Values;
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
                            needUpdate = true;
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
                        needUpdate = true;
                        var thisStorey = newStoreys.Find(c => c.Uid == addId);
                        var thisStoreyEntityIds = thisStorey.FloorEntityRelations.Select(c => c.Key).ToList();
                        var addEntitys = new Dictionary<string, THBimEntity>();
                        foreach (var id in thisStoreyEntityIds) 
                        {
                            addEntitys.Add(id, newEntitys[id]);
                        }
                        updateMeshIds.AddRange(thisStoreyEntityIds);
                        AddEntitys(addEntitys);
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
                        needUpdate = true;
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
                        needUpdate = true;
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
                        //var newRemovedUids = oldStorey.GetRemovedComponentUids(newStorey);
                        var newUpdatedUids = oldStorey.GetUpdatedComponentUids(newStorey);
                        //if (newRemovedUids.Count > 0)
                        //{
                        //    needUpdate = true;
                        //    RemoveEntitys(building, newRemovedUids);
                        //}
                        if (newAddedUids.Count > 0)
                        {
                            needUpdate = true;
                            updateMeshIds.AddRange(newAddedUids);
                            var addEntitys = newEntitys.Where(c => newAddedUids.Any(x => x == c.Key)).ToDictionary(c => c.Key, x => x.Value);
                            AddEntitys(building, addEntitys, true);
                        }
                        if (newUpdatedUids.Count > 0)
                        {
                            needUpdate = true;
                            updateMeshIds.AddRange(newUpdatedUids);
                            var updateEntitys = newEntitys.Where(c => newUpdatedUids.Any(x => x == c.Key)).ToDictionary(c => c.Key, x => x.Value);
                            UpdateEntitys(updateEntitys);
                        }
                    }
                }
                if (needUpdate) 
                {
                    UpdateCatchStorey();
                    UpateEntitySolidMesh(updateMeshIds);
                }
            }
            if (needUpdate)
                WriteToMidDataByFloor();
        }
        public void UpdateElement()
        {

        }
        public void ClearAllProject()
        {
            _allEntitys.Clear();
            _allBimProject.Clear();
            _allStoreys.Clear();
        }
        public void WriteToMidDataByFloor()
        {
            if (_allStoreys.Count < 1)
                return;
            var meshResult = new GeoMeshResult();
            var allStoreys = _allStoreys.Select(c => c.Value).ToList();
            Parallel.ForEach(allStoreys,new ParallelOptions(),storey=>
            {
                int pIndex = -1;
                var storeyGeoModels = new List<IfcMeshModel>();
                var storeyGeoPointNormals = new List<PointNormal>();
                foreach (var item in storey.FloorEntityRelations)
                {
                    var relation = item.Value;
                    if (null == relation)
                        continue;
                    var entity = _allEntitys[relation.RelationElementUid];
                    if (null == entity || entity.AllShapeGeometries.Count < 1)
                        continue;
                    if (UnShowEntityTypes.Contains(entity.FriendlyTypeName.ToString()))
                        continue;
                    var ptOffSet = storeyGeoPointNormals.Count();
                    var material = THBimMaterial.GetTHBimEntityMaterial(entity.FriendlyTypeName, true);
                    IfcMeshModel meshModel = new IfcMeshModel(relation.Id, entity.Id);
                    meshModel.TriangleMaterial = material;
                    foreach (var shapeGeo in entity.AllShapeGeometries) 
                    {
                        if(shapeGeo == null || shapeGeo.ShapeGeometry == null || string.IsNullOrEmpty(shapeGeo.ShapeGeometry.ShapeData))
                            continue;
                        var ms = new MemoryStream((shapeGeo.ShapeGeometry as IXbimShapeGeometryData).ShapeData);
                        var testData = ms.ToArray();
                        var br = new BinaryReader(ms);
                        var tr = br.ReadShapeTriangulation();
                        if (tr.Faces.Count < 1)
                            continue;
                        var moveVector = shapeGeo.ShapeGeometry.TempOriginDisplacement;
                        var transform = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
                        transform = relation.Matrix3D * storey.Matrix3D * transform * shapeGeo.Matrix3D;
                        var allPts = tr.Vertices.ToArray();
                        var allFace = tr.Faces;
                        foreach (var face in allFace.ToList())
                        {
                            var ptIndexs = face.Indices.ToArray();
                            for (int i = 0; i < face.TriangleCount; i++)
                            {
                                var triangle = new FaceTriangle();
                                var pt1Index = ptIndexs[i * 3];
                                var pt2Index = ptIndexs[i * 3 + 1];
                                var pt3Index = ptIndexs[i * 3 + 2];
                                var pt1 = allPts[pt1Index].TransPoint(transform);
                                var pt1Normal = face.Normals.Last().Normal;
                                if (pt1Index < face.Normals.Count())
                                    pt1Normal = face.Normals[pt1Index].Normal;
                                pIndex += 1;
                                triangle.ptIndex.Add(pIndex);
                                storeyGeoPointNormals.Add(GetPointNormal(pIndex, pt1, pt1Normal));
                                var pt2 = allPts[pt2Index].TransPoint(transform);
                                var pt2Normal = face.Normals.Last().Normal;
                                if (pt2Index < face.Normals.Count())
                                    pt2Normal = face.Normals[pt2Index].Normal;
                                pIndex += 1;
                                triangle.ptIndex.Add(pIndex);
                                storeyGeoPointNormals.Add(GetPointNormal(pIndex, pt2, pt2Normal));
                                var pt3 = allPts[pt3Index].TransPoint(transform);
                                var pt3Normal = face.Normals.Last().Normal;
                                if (pt3Index < face.Normals.Count())
                                    pt3Normal = face.Normals[pt3Index].Normal;
                                pIndex += 1;
                                triangle.ptIndex.Add(pIndex);
                                storeyGeoPointNormals.Add(GetPointNormal(pIndex, pt3, pt3Normal));
                                meshModel.FaceTriangles.Add(triangle);
                            }
                        }
                        
                    }
                    storeyGeoModels.Add(meshModel);
                }

                lock (meshResult) 
                {
                    int ptOffSet = meshResult.AllGeoPointNormals.Count;
                    foreach (var item in storeyGeoPointNormals)
                    {
                        item.PointIndex += ptOffSet;
                    }
                    foreach (var item in storeyGeoModels)
                    {
                        item.CIndex += ptOffSet;
                        foreach (var tr in item.FaceTriangles)
                        {
                            for (int i = 0; i < tr.ptIndex.Count; i++)
                                tr.ptIndex[i] += ptOffSet;
                        }
                    }
                    meshResult.AllGeoPointNormals.AddRange(storeyGeoPointNormals);
                    meshResult.AllGeoModels.AddRange(storeyGeoModels);
                }
            });
            DateTime start = DateTime.Now;
            var storeToEngineFile = new IfcStoreToEngineFile();
            storeToEngineFile.WriteMidDataMultithreading(meshResult.AllGeoModels, meshResult.AllGeoPointNormals);
            DateTime end = DateTime.Now;
            var totalTime = (end - start).TotalSeconds;
        }
        private void DeleteProjectData(string prjIdentity) 
        {
            THBimProject delPrj = null;
            foreach (var project in _allBimProject)
            {
                if (project.ProjectIdentity != prjIdentity)
                    continue;
                delPrj = project;
                //删除楼层记录，要删除的实体
                foreach (var build in project.ProjectSite.SiteBuildings) 
                {
                    foreach (var storey in build.Value.BuildingStoreys) 
                    {
                        foreach (var item in storey.Value.FloorEntitys) 
                        {
                            _allEntitys.Remove(item.Key);
                        }
                        _allStoreys.Remove(storey.Key);
                    }
                }
            }
            if (null != delPrj)
                _allBimProject.Remove(delPrj);
        }
        private bool IsAddProject(string prjIdentity) 
        {
            bool isAdd = true;
            foreach (var item in _allBimProject)
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
                if (!_allEntitys.ContainsKey(entityId))
                    continue;
                var entity = _allEntitys[entityId];
                var pid = entity.ParentUid;
                while (!string.IsNullOrEmpty(pid) && !thisStoreys.ContainsKey(pid))
                {
                    var pEntity = _allEntitys[pid];
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
                _allEntitys.Remove(rmId);
            }
        }
        private void AddEntitys(THBimBuilding building, Dictionary<string,THBimEntity> addEntitys,bool changeFloorData) 
        {
            var idOffSet = LastEntityIntId() + 1;
            foreach (var entityKeyValue in addEntitys)
            {
                var entity = entityKeyValue.Value;
                var pid = entity.ParentUid;
                while (!string.IsNullOrEmpty(pid) && !building.BuildingStoreys.ContainsKey(pid))
                {
                    //如果只是增加了门窗，父元素为墙，算更新数据，不算增的数据
                    var pEntity = addEntitys.ContainsKey(pid)? addEntitys[pid]:_allEntitys[pid];
                    pid = pEntity.ParentUid;
                }
                if (string.IsNullOrEmpty(pid) || !building.BuildingStoreys.ContainsKey(pid))
                    continue;
                entityKeyValue.Value.Id = idOffSet;
                _allEntitys.Add(entity.Uid, entity);
                foreach (var storeyKeyValue in building.BuildingStoreys)
                {
                    var storey = storeyKeyValue.Value;
                    if (storey.Uid != pid && storey.MemoryStoreyId != pid)
                        continue;
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
        private void AddEntitys(Dictionary<string, THBimEntity> addEntitys)
        {
            var idOffSet = LastEntityIntId() + 1;
            foreach (var entityKeyValue in addEntitys)
            {
                var entity = entityKeyValue.Value;
                entityKeyValue.Value.Id = idOffSet;
                _allEntitys.Add(entity.Uid, entity);
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
                var oldValue = _allEntitys[id];
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
                var entity = _allEntitys[id];
                updateEntitys.Add(entity);
                foreach (var item in entity.Openings)
                {
                    updateEntitys.Add(_allEntitys[item.Uid]);
                }
            }
            if (updateEntitys.Count < 1)
                return;
            convertFactory.CreateSolidMesh(updateEntitys);
        }
        private void AddProjectEntitys(Dictionary<string, THBimEntity> addBimEntitys) 
        {
            int idOffset = LastEntityIntId() + 1;
            foreach (var keyValue in addBimEntitys) 
            {
                keyValue.Value.Id += idOffset;
                _allEntitys.Add(keyValue.Key, keyValue.Value);
            }
        }
        private int LastEntityIntId() 
        {
            int idOffset = 0;
            if (_allEntitys.Count > 0)
                idOffset = _allEntitys.Last().Value.Id;
            return idOffset;
        }
        private PointNormal GetPointNormal(int pIndex, XbimPoint3D point, XbimVector3D normal)
        {
            return new PointNormal(pIndex, point, normal);
        }
        private void UpdateCatchStorey() 
        {
            _allStoreys.Clear();
            foreach (var project in _allBimProject) 
            {
                foreach (var build in project.ProjectSite.SiteBuildings) 
                {
                    foreach (var storeyKeyValue in build.Value.BuildingStoreys)
                        _allStoreys.Add(storeyKeyValue.Key, storeyKeyValue.Value);
                }
            }
        }
    }
    class GeoMeshResult 
    {
        public List<IfcMeshModel> AllGeoModels { get; }
        public List<PointNormal> AllGeoPointNormals { get; }
        public GeoMeshResult() 
        {
            AllGeoModels = new List<IfcMeshModel>();
            AllGeoPointNormals = new List<PointNormal>();
        }
    }
}
