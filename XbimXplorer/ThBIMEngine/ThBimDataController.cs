using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Model;
using THBimEngine.Geometry;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;

namespace XbimXplorer.ThBIMEngine
{
    class ThBimDataController
    {
        private Dictionary<string, THBimEntity> _allEntitys { get; }
        private Dictionary<string, THBimStorey> _allStoreys { get; }
        private List<ThTCHProject> _allProjects { get; }
        private List<THBimProject> _allBimProject { get; }
        public List<string> UnShowEntityTypes { get; }
        THEntityConvertFactory entityConvertFactory;
        public ThBimDataController()
        {
            _allStoreys = new Dictionary<string, THBimStorey>();
            _allProjects = new List<ThTCHProject>();
            _allBimProject = new List<THBimProject>();
            _allEntitys = new Dictionary<string, THBimEntity>();
            UnShowEntityTypes = new List<string>();
            UnShowEntityTypes.Add(typeof(THBimOpening).ToString());
            entityConvertFactory = new THEntityConvertFactory(Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3);
        }
        public void AddProject(ThTCHProject project)
        {
            bool isAdd = true;
            foreach (var item in _allProjects)
            {
                if (item.ProjectName == project.ProjectName)
                {
                    isAdd = false;
                    break;
                }
            }
            if (isAdd)
            {
                _allProjects.Add(project);
                var convertResult = entityConvertFactory.ThTCHProjectConvert(project);
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
                UpdateProject(project);
            }
        }
        public void DeleteProject()
        {

        }
        public void UpdateProject(ThTCHProject project)
        {
            var convertResult = entityConvertFactory.ThTCHProjectConvert(project); //ConvertProjectToTHBimProject(project, out Dictionary<string, THBimEntity> newEntitys);
            var newBimProject = convertResult.BimProject;
            var newEntitys = convertResult.ProjectEntitys;
            foreach (var item in _allBimProject)
            {
                var buildings = item.ProjectSite.SiteBuildings.Values;
                foreach (var building in buildings)
                {
                    var buildingId = building.Uid;
                    var id2BuildingDic = newBimProject.ProjectSite.SiteBuildings;
                    var newBuilding = id2BuildingDic[buildingId];

                    var newStoreyUids = new List<string>();
                    var storeyUids = new List<string>();
                    newStoreyUids = newBimProject.ProjectSite.SiteBuildings.First().Value.BuildingStoreys.Keys.ToList();
                    storeyUids = building.BuildingStoreys.Keys.ToList();
                    var newAddedUid = building.GetNewlyAddedComponentUids(newBuilding, newStoreyUids);
                    var newRemovedUid = building.GetRemovedComponentUids(newBuilding, storeyUids);
                    var newUpdatedUid = building.GetUpdatedComponentUids(newBuilding, newStoreyUids.Intersect(storeyUids).ToList());
                    ;
                }
            }
        }
        public void UpdateElement()
        {

        }
        public void ClearAllProject()
        {
            _allProjects.Clear();
            _allEntitys.Clear();
            _allBimProject.Clear();
        }
        public void WriteToMidFileByFloor(string midPath)
        {
            var meshResult = new GeoMeshResult();
            var allStoreys = _allStoreys.Select(c => c.Value).ToList();
            Parallel.ForEach(allStoreys,new ParallelOptions(),storey=>
            {
                int pIndex = 0;
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
            var storeToEngineFile = new IfcStoreToEngineFile();
            storeToEngineFile.WriteMidFile(meshResult.AllGeoModels, meshResult.AllGeoPointNormals, midPath);
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
