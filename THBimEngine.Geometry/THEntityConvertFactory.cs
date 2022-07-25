using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Model;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;

namespace THBimEngine.Geometry
{
    public class THEntityConvertFactory
    {
        private int _globalIndex = 0;
        List<THBimEntity> _allEntitys;
        THBimProject _bimProject;
        //GeometryFactory _geometryFactory;
        IfcSchemaVersion _schemaVersion;
        Dictionary<string, THBimStorey> _prjEntityFloors;
        Dictionary<string, THBimStorey> _allStoreys;
        
        public THEntityConvertFactory(IfcSchemaVersion ifcSchemaVersion)
        {
            _allEntitys = new List<THBimEntity>();
            _schemaVersion = ifcSchemaVersion;
            GeometryFactory _geometryFactory = new GeometryFactory(ifcSchemaVersion);
        }
        public ConvertResult ThTCHProjectConvert(ThTCHProject project)
        {
            ConvertResult convertResult = null;
            _prjEntityFloors = new Dictionary<string, THBimStorey>();
            _allStoreys = new Dictionary<string, THBimStorey>();
            _globalIndex = 0;
            _allEntitys = new List<THBimEntity>();
            _bimProject = null;
            //step1 转换几何数据
            ThTCHProjectToTHBimProject(project);
            
            //step2 转换每个实体的Solid;
            Parallel.ForEach(_allEntitys, new ParallelOptions(), entity =>
            {
                if (entity == null)
                    return;
                GeometryFactory _geometryFactory = new GeometryFactory(_schemaVersion);
                var solid = _geometryFactory.GetXBimSolid(entity.GeometryParam as GeometryStretch, XbimVector3D.Zero);
                if (null != solid && solid.SurfaceArea > 1)
                    entity.EntitySolids.Add(solid);
            });
            //step3 Solid剪切和Mesh
            Parallel.ForEach(_allEntitys, new ParallelOptions(), entity =>
            {
                if (entity == null)
                    return;
                GeometryFactory _geometryFactory = new GeometryFactory(_schemaVersion);
                var openingSolds = new List<IXbimSolid>();
                foreach (var opening in entity.Openings)
                {
                    if (opening.EntitySolids.Count < 1)
                        continue;
                    foreach (var solid in opening.EntitySolids)
                        openingSolds.Add(solid);
                }
                entity.ShapeGeometry = _geometryFactory.GetShapeGeometry(entity.EntitySolids, openingSolds);
            });

            var projectEntitys =_allEntitys.Where(c=>c!=null).ToDictionary(c => c.Uid, x => x);
            convertResult = new ConvertResult(_bimProject, _allStoreys, projectEntitys);

            return convertResult;
        }
        private void ThTCHProjectToTHBimProject(ThTCHProject project)
        {
            _allEntitys.Clear();
            _globalIndex = 0;
            if (null == project)
                return;
            _bimProject = new THBimProject(CurrentGIndex(), project.ProjectName, "", project.Uuid);
            AddElementIndex();
            var bimSite = new THBimSite(CurrentGIndex(), "", "", project.Site.Uuid);
            AddElementIndex();
            var bimBuilding = new THBimBuilding(CurrentGIndex(), project.Site.Building.BuildingName, "", project.Site.Building.Uuid);
            foreach (var storey in project.Site.Building.Storeys)
            {
                var bimStorey = new THBimStorey(CurrentGIndex(), storey.Number, storey.Elevation, storey.Height, "", storey.Uuid);
                AddElementIndex();
                if (!string.IsNullOrEmpty(storey.MemoryStoreyId))
                {
                    var memoryStorey = _prjEntityFloors[storey.MemoryStoreyId];
                    foreach (var keyValue in memoryStorey.FloorEntitys)
                    {
                        var relation = keyValue.Value;
                        if (null == relation)
                            continue;
                        var entityRelation = new THBimElementRelation(CurrentGIndex(), relation.Name);
                        AddElementIndex();
                        entityRelation.ParentUid = bimStorey.Uid;
                        entityRelation.RelationElementUid = relation.RelationElementUid;
                        entityRelation.RelationElementId = relation.RelationElementId;
                        bimStorey.FloorEntitys.Add(entityRelation.Uid,entityRelation);
                    }
                }
                else 
                {
                    //多线程有少数据导致后面报错，后续再处理
                    var moveVector = storey.Origin.Point3D2Vector();
                    Parallel.ForEach(storey.Walls, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, wall =>
                    {
                        var bimWall = new THBimWall(CurrentGIndex(), string.Format("wall#{0}", CurrentGIndex()), wall.THTCHGeometryParam(), "", wall.Uuid);
                        
                        var wallRelation = new THBimElementRelation(bimWall.Id, bimWall.Name,bimWall, bimWall.Describe, bimWall.Uid);
                        lock (bimStorey)
                        {
                            bimStorey.FloorEntitys.Add(bimWall.Uid, wallRelation);
                        }
                        bimWall.ParentUid = bimStorey.Uid;
                        AddElementIndex();
                        
                        if (null != wall.Doors)
                        {
                            foreach (var door in wall.Doors)
                            {
                                var bimDoor = new THBimDoor(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), door.THTCHGeometryParam(), "", door.Uuid);
                                var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor,bimDoor.Describe, bimDoor.Uid);
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntitys.Add(bimDoor.Uid, doorRelation);
                                }
                                lock (_allEntitys)
                                {
                                    _allEntitys.Add(bimDoor);
                                }
                                AddElementIndex();
                            }
                        }
                        if (null != wall.Windows)
                        {
                            foreach (var window in wall.Windows)
                            {
                                var bimWindow = new THBimWindow(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), window.THTCHGeometryParam(), "", window.Uuid);
                                bimWindow.ParentUid = bimWall.Uid;
                                var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name,bimWindow, bimWindow.Describe, bimWindow.Uid);
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntitys.Add(bimWindow.Uid, windowRelation);
                                }
                                lock (_allEntitys)
                                {
                                    _allEntitys.Add(bimWindow);
                                }
                                AddElementIndex();
                            }
                        }
                        if (null != wall.Openings)
                        {
                            foreach (var opening in wall.Openings)
                            {
                                var bimOpening = new THBimOpening(CurrentGIndex(), string.Format("opening#{0}", CurrentGIndex()), opening.THTCHGeometryParam(), "", opening.Uuid);
                                bimOpening.ParentUid = bimWall.Uid;
                                var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name,bimOpening, bimOpening.Describe, bimOpening.Uid);
                                lock (bimStorey)
                                {
                                    _allEntitys.Add(bimOpening);
                                }
                                bimWall.Openings.Add(bimOpening);
                                AddElementIndex();
                            }
                        }
                        lock (_allEntitys)
                        {
                            _allEntitys.Add(bimWall);
                        }
                    });
                    Parallel.ForEach(storey.Slabs, new ParallelOptions() { MaxDegreeOfParallelism=1}, slab =>
                    {
                        var bimSlab = new THBimSlab(CurrentGIndex(), string.Format("slab#{0}", CurrentGIndex()), slab.SlabGeometryParam(), "", slab.Uuid);
                        var wallRelation = new THBimElementRelation(bimSlab.Id, bimSlab.Name, bimSlab, bimSlab.Describe, bimSlab.Uid);
                        bimStorey.FloorEntitys.Add(bimSlab.Uid, wallRelation);
                        bimSlab.ParentUid = bimStorey.Uid;
                        AddElementIndex();
                        lock (_allEntitys)
                        {
                            _allEntitys.Add(bimSlab);
                        }
                    });
                    Parallel.ForEach(storey.Railings, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, railing => 
                    {
                        var bimRailing = new THBimRailing(CurrentGIndex(), string.Format("railing#{0}", CurrentGIndex()), railing.THTCHGeometryParam(), "", railing.Uuid);
                        var wallRelation = new THBimElementRelation(bimRailing.Id, bimRailing.Name, bimRailing, bimRailing.Describe, bimRailing.Uid);
                        bimStorey.FloorEntitys.Add(bimRailing.Uid, wallRelation);
                        bimRailing.ParentUid = bimStorey.Uid;
                        AddElementIndex();
                        lock (_allEntitys)
                        {
                            _allEntitys.Add(bimRailing);
                        }
                    });
                    _prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                }
                _allStoreys.Add(bimStorey.Uid, bimStorey);
                bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
            }
            bimSite.SiteBuildings.Add(bimBuilding.Uid,bimBuilding);
            _bimProject.ProjectSite = bimSite;
        }

        private void AddElementIndex(int addCount = 1)
        {
            _globalIndex += addCount;
        }
        private int CurrentGIndex()
        {
            return _globalIndex;
        }
    }

    public class ConvertResult 
    {
        public THBimProject BimProject { get; set; }
        public Dictionary<string,THBimEntity> ProjectEntitys { get; }
        public Dictionary<string,THBimStorey> ProjectStoreys { get; }
        public ConvertResult() 
        {
            ProjectEntitys = new Dictionary<string, THBimEntity>();
            ProjectStoreys = new Dictionary<string, THBimStorey>();
        }
        public ConvertResult(THBimProject bimProject, Dictionary<string,THBimStorey> storeys, Dictionary<string,THBimEntity> entitys) : this()
        {
            BimProject = bimProject;
            if (null != storeys && storeys.Count > 0) 
            {
                foreach (var item in storeys) 
                {
                    if (item.Value == null)
                        continue;
                    ProjectStoreys.Add(item.Key,item.Value);
                }
            }
            if (null != entitys && entitys.Count > 0) 
            {
                foreach (var item in entitys) 
                {
                    if (item.Value == null)
                        continue;
                    ProjectEntitys.Add(item.Key,item.Value);
                }
            }
        }
    }
}
