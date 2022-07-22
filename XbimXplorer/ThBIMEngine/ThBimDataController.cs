using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private int _globalIndex = 0;
        private Dictionary<string, THBimEntity> _allEntitys { get; }
        private List<ThTCHProject> _allProjects { get; }
        private List<THBimProject> _allBimProject { get; }
        GeometryFactory _geometryFactory;
        public ThBimDataController(List<ThTCHProject> projects) 
        {
            _allProjects = new List<ThTCHProject>();
            _allBimProject = new List<THBimProject>();
            _allEntitys = new Dictionary<string, THBimEntity>();
            _globalIndex = 0;
            _geometryFactory = new GeometryFactory(Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3);
            ConvertProjectToTHBimProject(projects);
            foreach (var project in _allBimProject) 
            {
                foreach (var building in project.ProjectSite.SiteBuildings) 
                {
                    foreach (var storey in building.BuildingStoreys.Values) 
                    {
                        if (string.IsNullOrEmpty(storey.MemoryStoreyId))
                        {
                            MeshBimEntity(storey, _allEntitys);
                        }
                        else 
                        {
                            
                        }
                    }
                }
            }
        }
        public void AddProject(ThTCHProject project) 
        {
            bool isAdd = true;
            foreach (var item in _allProjects) 
            {
                if(item.ProjectName == project.ProjectName)
                {
                    isAdd = false;
                    break;
                }
            }
            if (isAdd)
            {
                _allProjects.Add(project);
                ConvertProjectToTHBimProject(new List<ThTCHProject> { project });
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
            THBimProject newBimProject = new THBimProject(CurrentGIndex(), project.ProjectName);
            foreach (var item in _allBimProject)
            {
                var buildings = item.ProjectSite.SiteBuildings;
                foreach(var building in buildings)
                {
                    var buildingId = building.Uid;
                    var id2BuildingDic = newBimProject.ProjectSite.SiteBuildings.ToDictionary(b => b.Uid);
                    var newBuilding = id2BuildingDic[buildingId];

                    var newStoreyUids = new List<string>();
                    var storeyUids = new List<string>();
                    

                    var newAddedUid = building.GetNewlyAddedComponentUids(newBuilding, newStoreyUids.Except(storeyUids).ToList());
                    var newRemovedUid = building.GetRemovedComponentUids(storeyUids.Except(newStoreyUids).ToList());
                    var newUpdatedUid = building.GetUpdatedComponentUids(newBuilding, newStoreyUids.Union(storeyUids).ToList());

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
            _globalIndex = 0;
        }

        public void WriteToMidFile(string midPath) 
        {
            var allGeoModels = new List<IfcMeshModel>();
            var allGeoPointNormals = new List<PointNormal>();
            int pIndex = 0;
            foreach (var item in _allEntitys) 
            {
                var entity = item.Value;
                if (null == entity || entity.ShapeGeometry == null)
                    continue;
                var ptOffSet = allGeoPointNormals.Count();
                var ms = new MemoryStream((entity.ShapeGeometry as IXbimShapeGeometryData).ShapeData);
                var testData = ms.ToArray();
                var br = new BinaryReader(ms);
                var tr = br.ReadShapeTriangulation();
                if (tr.Faces.Count < 1)
                    continue;
                var moveVector = entity.ShapeGeometry.TempOriginDisplacement;
                var transform = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
                var material = GetMeshModelMaterial("wall");
                IfcMeshModel meshModel = new IfcMeshModel(entity.Id, entity.Id);
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
                        var pt1 = TransPoint(allPts[pt1Index],transform);
                        var pt1Normal = face.Normals.Last().Normal;
                        if (pt1Index < face.Normals.Count())
                            pt1Normal = face.Normals[pt1Index].Normal;
                        pIndex += 1;
                        triangle.ptIndex.Add(pIndex);
                        allGeoPointNormals.Add(GetPointNormal(pIndex, pt1, pt1Normal));
                        var pt2 = TransPoint(allPts[pt2Index], transform); ;
                        var pt2Normal = face.Normals.Last().Normal;
                        if (pt2Index < face.Normals.Count())
                            pt2Normal = face.Normals[pt2Index].Normal;
                        pIndex += 1;
                        triangle.ptIndex.Add(pIndex);
                        allGeoPointNormals.Add(GetPointNormal(pIndex, pt2, pt2Normal));
                        var pt3 = TransPoint(allPts[pt3Index], transform); ;
                        var pt3Normal = face.Normals.Last().Normal;
                        if (pt3Index < face.Normals.Count())
                            pt3Normal = face.Normals[pt3Index].Normal;
                        pIndex += 1;
                        triangle.ptIndex.Add(pIndex);
                        allGeoPointNormals.Add(GetPointNormal(pIndex, pt3, pt3Normal));
                        meshModel.FaceTriangles.Add(triangle);
                    }
                }
                allGeoModels.Add(meshModel);
            }
            var storeToEngineFile = new IfcStoreToEngineFile();
            storeToEngineFile.WriteMidFile(allGeoModels, allGeoPointNormals, midPath);
        }
        
        private void ConvertProjectToTHBimProject(List<ThTCHProject> projects) 
        {
            if (null == projects || projects.Count < 1)
                return;
            foreach (var project in projects) 
            {
                THBimProject bimProject = new THBimProject(CurrentGIndex(), project.ProjectName);
                AddElementIndex();
                THBimSite bimSite = new THBimSite(CurrentGIndex(), "");
                AddElementIndex();
                THBimBuilding bimBuilding = new THBimBuilding(CurrentGIndex(), project.Site.Building.BuildingName);
                foreach (var storey in project.Site.Building.Storeys) 
                {
                    var moveVector = storey.Origin.Point3D2Vector();
                    var bimStory = new THBimStorey(CurrentGIndex(), storey.Number, storey.Elevation, storey.Height);
                    AddElementIndex();
                    foreach (var wall in storey.Walls) 
                    {
                        var bimWall = new THBimWall(CurrentGIndex(), string.Format("wall#{0}", CurrentGIndex()), wall.WallGeometryParam());
                        var wallRelation = new THBimElementRelation(bimWall.Id, bimWall.Name, bimWall.Describe, bimWall.Uid);
                        bimStory.FloorEntitys.Add(bimWall.Uid, wallRelation);
                        bimWall.ParentUid = bimStory.Uid;
                        AddElementIndex();
                        _allEntitys.Add(bimWall.Uid, bimWall);
                        var openingSolids = new List<IXbimSolid>();
                        if (null != wall.Doors) 
                        {
                            foreach (var door in wall.Doors)
                            {
                                var bimDoor = new THBimDoor(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), door.DoorGeometryParam());
                                bimDoor.ParentUid = bimWall.Uid;
                                bimDoor.ShapeGeometry = _geometryFactory.GetShapeGeometry(bimDoor.GeometryParam as GeometryStretch, moveVector,out IXbimSolid doorSolid);
                                if (null != doorSolid && doorSolid.SurfaceArea > 10)
                                    openingSolids.Add(doorSolid);
                                var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor.Describe, bimDoor.Uid);
                                bimStory.FloorEntitys.Add(bimDoor.Uid, doorRelation);
                                _allEntitys.Add(bimDoor.Uid, bimDoor);
                                AddElementIndex();
                            }
                        }
                        if (null != wall.Windows) 
                        {
                            foreach (var window in wall.Windows)
                            {
                                var bimWindow = new THBimWindow(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), window.WindowGeometryParam());
                                bimWindow.ParentUid = bimWall.Uid;
                                bimWindow.ShapeGeometry = _geometryFactory.GetShapeGeometry(bimWindow.GeometryParam as GeometryStretch, moveVector, out IXbimSolid windowSolid);
                                if (null != windowSolid && windowSolid.SurfaceArea > 10)
                                    openingSolids.Add(windowSolid);
                                var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name, bimWindow.Describe, bimWindow.Uid);
                                bimStory.FloorEntitys.Add(bimWindow.Uid, windowRelation);
                                _allEntitys.Add(bimWindow.Uid, bimWindow);
                                AddElementIndex();
                            }
                        }
                        if (null != wall.Openings)
                        {
                            foreach (var opening in wall.Openings)
                            {
                                var bimOpening = new THBimOpening(CurrentGIndex(), string.Format("opening#{0}", CurrentGIndex()), opening.OpeningGeometryParam());
                                bimOpening.ParentUid = bimWall.Uid;
                                bimOpening.ShapeGeometry = _geometryFactory.GetShapeGeometry(bimOpening.GeometryParam as GeometryStretch, moveVector, out IXbimSolid openingSolid);
                                if (null != openingSolid && openingSolid.SurfaceArea > 10)
                                    openingSolids.Add(openingSolid);
                                var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening.Describe, bimOpening.Uid);
                                bimStory.FloorEntitys.Add(bimOpening.Uid, openingRelation);
                                _allEntitys.Add(bimOpening.Uid, bimOpening);
                                AddElementIndex();
                            }
                        }

                        bimWall.ShapeGeometry = _geometryFactory.GetShapeGeometry(bimWall.GeometryParam as GeometryStretch, moveVector, openingSolids);
                    }
                    bimBuilding.BuildingStoreys.Add(bimStory.Uid, bimStory);
                }
                bimSite.SiteBuildings.Add(bimBuilding);
                bimProject.ProjectSite = bimSite;
                _allBimProject.Add(bimProject);
            }
        }
        private void MeshBimEntity(THBimStorey storey,Dictionary<string,THBimEntity> bimEntities) 
        {
            //var moveVector = storey.Origin.Point3D2Vector();
            //foreach (var item in storey.FloorEntitys) 
            //{
            //    var realtion = item.Value;
            //    var entity = bimEntities[realtion.Uid];
            //    bimEntities[realtion.Uid].ShapeGeometry = _geometryFactory.GetShapeGeometry(entity.GeometryParam as GeometryStretch, moveVector);
            //}
            
        }
        private XbimPoint3D TransPoint(XbimPoint3D xbimPoint, XbimMatrix3D xbimMatrix)
        {
            return xbimMatrix.Transform(xbimPoint);
        }
        private void AddElementIndex(int addCount =1) 
        {
            _globalIndex += addCount;
        }
        private int CurrentGIndex() 
        {
            return _globalIndex;
        }
        private IfcMaterial GetMeshModelMaterial(string typeStr)
        {
            var defalutMaterial = new IfcMaterial
            {
                Kd_R = 169 / 255f,
                Kd_G = 179 / 255f,
                Kd_B = 218 / 255f,
                Ks_R = 0,
                Ks_B = 0,
                Ks_G = 0,
                K = 0.5f,
                NS = 12,
            };
            if (typeStr.Contains("window"))
            {

            }
            else if (typeStr.Contains("open"))
            {

            }
            /*
			defalutMaterial = new IfcMaterial
			{
				Kd_R = v.Red,
				Kd_G = v.Green,
				Kd_B = v.Blue,
				Ks_R = v.DiffuseFactor,
				Ks_G = v.SpecularFactor,
				Ks_B = v.DiffuseTransmissionFactor,
				K = v.Alpha,
				NS = 12,
			};
			return defalutMaterial;*/

            //testListSting.Add(ifcModel.GetType().ToString().ToLower());
            //testTypeStr.Add(typeStr);
            if (typeStr.Contains("wall"))
            {
                defalutMaterial = new IfcMaterial
                {
                    Kd_R = 226 / 255f,
                    Kd_G = 212 / 255f,
                    Kd_B = 190 / 255f,
                    Ks_R = 0,
                    Ks_B = 0,
                    Ks_G = 0,
                    K = 1f,
                    NS = 12,
                };
            }
            else if (typeStr.Contains("beam"))
            {
                defalutMaterial = new IfcMaterial
                {
                    Kd_R = 194 / 255f,
                    Kd_G = 178 / 255f,
                    Kd_B = 152 / 255f,
                    Ks_R = 0,
                    Ks_B = 0,
                    Ks_G = 0,
                    K = 1f,
                    NS = 12,
                };
            }
            else if (typeStr.Contains("door"))
            {
                defalutMaterial = new IfcMaterial
                {
                    Kd_R = 167 / 255f,
                    Kd_G = 182 / 255f,
                    Kd_B = 199 / 255f,
                    Ks_R = 0,
                    Ks_B = 0,
                    Ks_G = 0,
                    K = 1f,
                    NS = 12,
                };
            }
            else if (typeStr.Contains("slab"))
            {
                defalutMaterial = new IfcMaterial
                {
                    Kd_R = 167 / 255f,
                    Kd_G = 182 / 255f,
                    Kd_B = 199 / 255f,
                    Ks_R = 0,
                    Ks_B = 0,
                    Ks_G = 0,
                    K = 1f,
                    NS = 12,
                };
            }
            else if (typeStr.Contains("window"))
            {
                defalutMaterial = new IfcMaterial
                {
                    Kd_R = 116 / 255f,
                    Kd_G = 195 / 255f,
                    Kd_B = 219 / 255f,
                    Ks_R = 0,
                    Ks_B = 0,
                    Ks_G = 0,
                    K = 0.5f,
                    NS = 12,
                };
            }
            else if (typeStr.Contains("column"))
            {
                defalutMaterial = new IfcMaterial
                {
                    Kd_R = 171 / 255f,
                    Kd_G = 157 / 255f,
                    Kd_B = 135 / 255f,
                    Ks_R = 0,
                    Ks_B = 0,
                    Ks_G = 0,
                    K = 1f,
                    NS = 12,
                };
            }
            else if (typeStr.Contains("railing"))
            {
                defalutMaterial = new IfcMaterial { Kd_R = 136 / 255f, Kd_G = 211 / 255f, Kd_B = 198 / 255f, Ks_R = 0, Ks_B = 0, Ks_G = 0, K = 0.5f, NS = 12, };
            }
            else if (typeStr.Contains("open"))
            {

            }
            else if (typeStr.Contains("ifcmaterial"))
            {

            }
            else
            {

            }
            return defalutMaterial;
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
}
