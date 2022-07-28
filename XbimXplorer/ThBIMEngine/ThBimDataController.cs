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
        //private List<ThTCHProject> _allProjects { get; }
        private List<THBimProject> _allBimProject { get; }
        public List<string> UnShowEntityTypes { get; }
        ConvertFactoryBase convertFactory;
        public ThBimDataController()
        {
            _allStoreys = new Dictionary<string, THBimStorey>();
            //_allProjects = new List<ThTCHProject>();
            _allBimProject = new List<THBimProject>();
            _allEntitys = new Dictionary<string, THBimEntity>();
            UnShowEntityTypes = new List<string>();
            UnShowEntityTypes.Add(typeof(THBimOpening).ToString());
        }
        public void AddProject(ThTCHProject project)
        {
            convertFactory = new THProjectConvertFactory(Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3);
            bool isAdd = true;
            foreach (var item in _allBimProject)
            {
                if (item.Name == project.ProjectName)
                {
                    isAdd = false;
                    break;
                }
            }
            if (isAdd)
            {
                //_allProjects.Add(project);
                var convertResult = convertFactory.ProjectConvert(project,true);
                if (null != convertResult) 
                {
                    _allBimProject.Add(convertResult.BimProject);
                    AddProjectEntitys(convertResult.ProjectEntitys);
                    foreach (var item in convertResult.ProjectStoreys) 
                    {
                        _allStoreys.Add(item.Key, item.Value);
                    }
                }
            }
            else
            {
                var convertResult = convertFactory.ProjectConvert(project, false);
                UpdateProject(convertResult);
            }
        }
        public void AddProject(IfcStore ifcStore) 
        {
            convertFactory = new THProjectConvertFactory(ifcStore.IfcSchemaVersion);
            throw new System.NotSupportedException();
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
                foreach (var building in buildings)
                {
                    var buildingId = building.Uid;
                    var id2BuildingDic = newBimProject.ProjectSite.SiteBuildings;
                    var newBuilding = id2BuildingDic[buildingId];
                    #region 楼层变化的相关信息（只比较楼层的标准非标准信息，不比较内部的实体信息）
                    /*
                     楼层变化的信息比较复杂
                    1、楼层增加、删除 1，2，3  =》1，2，3，4
                    2、标准层的增加、删除 1，2，3-10 =》 1，2，3-14  =》1，2，3-7，8-10 =》1，2，3-5，6
                    3、标准层的变化 1，2，3-10 =》1，2，3，4-12
                    可能出现组合情况
                     */
                    #endregion
                    /*
                    var oldStoreys = building.BuildingStoreys.Values.ToList();
                    var newStoreys = newBuilding.BuildingStoreys.Values.ToList();
                    //step1 删除原标准层首层和非标层 删除的数据
                    foreach (var storey in building.BuildingStoreys.Values.ToList()) 
                    {
                        if (!string.IsNullOrEmpty(storey.MemoryStoreyId))
                            continue;
                        var newRemovedUids = new List<string>();
                        if (newBuilding.BuildingStoreys.ContainsKey(storey.Uid))
                        {
                            //楼层已经删除
                            newRemovedUids.AddRange(storey.FloorEntitys.Select(c => c.Key).ToList());
                        }
                        else
                        {
                            var newStorey = newBuilding.BuildingStoreys[storey.Uid];
                            if (string.IsNullOrEmpty(newStorey.MemoryStoreyId))
                            {
                                //楼层变为标准层非首层数据
                                newRemovedUids.AddRange(storey.FloorEntitys.Select(c => c.Key).ToList());
                            }
                            else
                            {
                                newRemovedUids = storey.GetRemovedComponentUids(newStorey);
                            }
                        }
                        if (newRemovedUids.Count > 0)
                        {
                            RemoveEntitys(prjId, newRemovedUids);
                        }
                    }
                    //step2 处理删除的楼层
                    var oldUids = oldStoreys.Select(c => c.Uid).ToList();
                    var newUids = newStoreys.Select(c => c.Uid).ToList();
                    var rmAllUids = oldUids.Except(newUids).ToList();
                    RemoveStoreys(prjId, rmAllUids);
                    //step3 处理添加非标和
                    var oldNoMemoryUids = oldStoreys.Where(c => string.IsNullOrEmpty(c.MemoryStoreyId)).Select(c => c.Uid).ToList();
                    var newNoMemoryUids = newStoreys.Where(c => string.IsNullOrEmpty(c.MemoryStoreyId)).Select(c => c.Uid).ToList();
                    //判断是否有变化,先处理非标层和标准层首层的增加
                    var addAllUids = newUids.Except(oldUids);
                    var addNoMemoryUids = newNoMemoryUids.Except(oldNoMemoryUids);
                    foreach (var addId in addNoMemoryUids)
                    {
                        if (_allStoreys.ContainsKey(addId))
                        {
                            //标准层非首层变为非标层或标准层的首层
                        }
                        else
                        {
                            //新增的非标层或新增的标准层首层
                            var thisStorey = newStoreys.Find(c => c.Uid == addId);
                            var thisStoreyEntityIds = thisStorey.FloorEntitys.Select(c => c.Key).ToList();
                        }
                    }*/

                    var newStoreyUids = new List<string>();
                    var storeyUids = new List<string>();
                    newStoreyUids = newBimProject.ProjectSite.SiteBuildings.First().Value.BuildingStoreys.Keys.ToList();
                    storeyUids = building.BuildingStoreys.Keys.ToList();
                    var newAddedUids = building.GetNewlyAddedComponentUids(newBuilding, newStoreyUids);
                    var newRemovedUids = building.GetRemovedComponentUids(newBuilding, storeyUids);
                    var newUpdatedUids = building.GetUpdatedComponentUids(newBuilding, newStoreyUids.Intersect(storeyUids).ToList());
                    var idOffSet = LastEntityIntId() + 1;
                    if (newRemovedUids.Count > 0)
                    {
                        needUpdate = true;
                        RemoveEntitys(prjId, newRemovedUids);
                    }
                    var updateMeshIds = new List<string>();
                    if (newAddedUids.Count > 0) 
                    {
                        needUpdate = true;
                        updateMeshIds.AddRange(newAddedUids);
                        var addEntitys = newEntitys.Where(c => newAddedUids.Any(x => x == c.Key)).ToDictionary(c => c.Key, x => x.Value);
                        AddEntitys(prjId, addEntitys);
                    }
                    if (newUpdatedUids.Count > 0)
                    {
                        needUpdate = true;
                        updateMeshIds.AddRange(newUpdatedUids);
                        var updateEntitys = newEntitys.Where(c => newUpdatedUids.Any(x => x == c.Key)).ToDictionary(c => c.Key, x => x.Value);
                        UpdateEntitys(prjId, updateEntitys);
                    }
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
            //_allProjects.Clear();
            _allEntitys.Clear();
            _allBimProject.Clear();
        }
        public void WriteToMidDataByFloor()
        {
            var meshResult = new GeoMeshResult();
            var allStoreys = _allStoreys.Select(c => c.Value).ToList();
            Parallel.ForEach(allStoreys,new ParallelOptions(),storey=>
            {
                int pIndex = -1;
                var storeyGeoModels = new List<IfcMeshModel>();
                var storeyGeoPointNormals = new List<PointNormal>();
                var storeyMoveVector = (new XbimPoint3D(0, 0, storey.Elevation)).Point3D2Vector();
                foreach (var item in storey.FloorEntitys)
                {
                    var relation = item.Value;
                    if (null == relation)
                        continue;
                    var entity = _allEntitys[relation.RelationElementUid];
                    if (null == entity || entity.ShapeGeometry == null || string.IsNullOrEmpty(entity.ShapeGeometry.ShapeData))
                        continue;
                    var ptOffSet = storeyGeoPointNormals.Count();
                    var ms = new MemoryStream((entity.ShapeGeometry as IXbimShapeGeometryData).ShapeData);
                    var testData = ms.ToArray();
                    var br = new BinaryReader(ms);
                    var tr = br.ReadShapeTriangulation();
                    if (tr.Faces.Count < 1)
                        continue;
                    var moveVector = entity.ShapeGeometry.TempOriginDisplacement + storeyMoveVector;
                    var transform = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
                    var material = THBimMaterial.GetTHBimEntityMaterial(entity.GetType());
                    IfcMeshModel meshModel = new IfcMeshModel(relation.Id, entity.Id);
                    var allPts = tr.Vertices.ToArray();
                    var allFace = tr.Faces;
                    foreach (var face in allFace.ToList())
                    {
                        var ptIndexs = face.Indices.ToArray();
                        for (int i = 0; i < face.TriangleCount; i++)
                        {
                            var triangle = new FaceTriangle();
                            triangle.TriangleMaterial = material;
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

        private void RemoveStoreys(string prjId, List<string> rmStoreyIds) 
        {
            foreach (var item in rmStoreyIds)
                _allStoreys.Remove(item);
            foreach (var storeyKeyValue in _allStoreys)
            {
                var storey = storeyKeyValue.Value;
                if (rmStoreyIds.Contains(storey.MemoryStoreyId)) 
                {
                    storey.FloorEntitys.Clear();
                    storey.MemoryStoreyId = string.Empty;
                }
            }
        }
        private void RemoveEntitys(string prjId, List<string> rmEntityIds) 
        {
            if (null == rmEntityIds || rmEntityIds.Count < 1)
                return;
            var rmIds = new List<string>();
            foreach (var entityId in rmEntityIds)
            {
                if (!_allEntitys.ContainsKey(entityId))
                    continue;
                var entity = _allEntitys[entityId];
                var pid = entity.ParentUid;
                while (!string.IsNullOrEmpty(pid) && !_allStoreys.ContainsKey(pid))
                {
                    var pEntity = _allEntitys[pid];
                    pid = pEntity.ParentUid;
                }
                if (string.IsNullOrEmpty(pid))
                    continue;
                rmIds.Add(entityId);
                foreach (var storeyKeyValue in _allStoreys)
                {
                    var storey = storeyKeyValue.Value;
                    if (storey.Uid != pid && storey.MemoryStoreyId != pid)
                        continue;
                    var rmRealtion = storey.FloorEntitys.Where(c => c.Value.RelationElementUid == entityId).Select(c => c.Key).ToList();
                    foreach (var rmId in rmRealtion)
                    {
                        storey.FloorEntitys.Remove(rmId);
                    }
                }
            }
            foreach (var rmId in rmIds) 
            {
                _allEntitys.Remove(rmId);
            }
        }
        private void AddEntitys(string prjId, Dictionary<string,THBimEntity> addEntitys) 
        {
            var idOffSet = LastEntityIntId() + 1;
            foreach (var entityKeyValue in addEntitys)
            {
                var entity = entityKeyValue.Value;
                var pid = entity.ParentUid;
                while (!string.IsNullOrEmpty(pid) && !_allStoreys.ContainsKey(pid))
                {
                    var pEntity = addEntitys[pid];
                    pid = pEntity.ParentUid;
                }
                if (string.IsNullOrEmpty(pid) || !_allStoreys.ContainsKey(pid))
                    continue;
                _allEntitys.Add(entity.Uid, entity);
                foreach (var storeyKeyValue in _allStoreys)
                {
                    var storey = storeyKeyValue.Value;
                    if (storey.Uid != pid && storey.MemoryStoreyId != pid)
                        continue;
                   
                    var addRelation = new THBimElementRelation(idOffSet, "#wall");
                    addRelation.ParentUid = storey.Uid;
                    addRelation.RelationElementUid = entity.Uid;
                    addRelation.RelationElementId = entity.Id;
                    storey.FloorEntitys.Add(addRelation.Uid, addRelation);
                    idOffSet += 1;
                }
            }
        }
        private void UpdateEntitys(string prjId, Dictionary<string, THBimEntity> updateEntitys)
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
                oldValue.ShapeGeometry = null;
            }
        }
        private void UpateEntitySolidMesh(List<string> updateEntityIds) 
        {
            if (null == updateEntityIds || updateEntityIds.Count < 1)
                return;
            List<THBimEntity> updateEntitys = new List<THBimEntity>();
            foreach (var id in updateEntityIds) 
            {
                if (!_allEntitys.ContainsKey(id))
                    continue;
                updateEntitys.Add(_allEntitys[id]);
            }
            if (updateEntitys.Count < 1)
                return;
            convertFactory.CreateSolidMesh(updateEntitys);
        }
        private void AddProjectEntitys(Dictionary<string, THBimEntity> addBimEntitys) 
        {
            int idOffset = 0;
            if (_allEntitys.Count > 0)
                idOffset= _allEntitys.Last().Value.Id+1;
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
            return new PointNormal
            {
                PointIndex = pIndex,
                Point = new PointVector() { X = -(float)point.X, Y = (float)point.Z, Z = (float)point.Y },
                Normal = new PointVector() { X = -(float)normal.X, Y = (float)normal.Z, Z = (float)normal.Y },
            };
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
