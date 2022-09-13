using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Model;
using THBimEngine.Geometry.NTS;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;

namespace THBimEngine.Geometry.ProjectFactory
{
    public class THProjectConvertFactory : ConvertFactoryBase
    {
        public THProjectConvertFactory(IfcSchemaVersion ifcSchemaVersion) : base(ifcSchemaVersion)
        {
        }
        public override ConvertResult ProjectConvert(object objProject, bool createSolidMesh)
        {
            var project = objProject as ThTCHProject;
            if (null == project)
                throw new System.NotSupportedException();
            ConvertResult convertResult = null;
            //step1 转换几何数据
            ThTCHProjectToTHBimProject(project);
            bimProject.ProjectIdentity = project.Uuid;
            if (createSolidMesh)
            {
                CreateSolidMesh(allEntitys.Values.ToList());
            }
            foreach (var item in allStoreys)
            {
                bimProject.PrjAllStoreys.Add(item.Key, item.Value);
                foreach (var entity in item.Value.FloorEntitys)
                {
                    bimProject.PrjAllEntitys.Add(entity.Key, entity.Value);
                }
                foreach (var relation in item.Value.FloorEntityRelations)
                {
                    bimProject.PrjAllRelations.Add(relation.Key, relation.Value);
                }
            }
            convertResult = new ConvertResult(bimProject, allStoreys, allEntitys);
            return convertResult;
        }
        private void ThTCHProjectToTHBimProject(ThTCHProject project)
        {
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project)
                return;
            bimProject = new THBimProject(CurrentGIndex(), project.ProjectName, "", project.Uuid);
            bimProject.ProjectIdentity = project.Uuid;
            var bimSite = new THBimSite(CurrentGIndex(), "", "", project.Site.Uuid);
            var bimBuilding = new THBimBuilding(CurrentGIndex(), project.Site.Building.BuildingName, "", project.Site.Building.Uuid);
            foreach (var storey in project.Site.Building.Storeys)
            {
                var bimStorey = new THBimStorey(CurrentGIndex(), storey.Number, storey.Elevation, storey.Height, "", storey.Uuid);
                bimStorey.Matrix3D = XbimMatrix3D.CreateTranslation(storey.Origin.Point3D2Vector());
                if (!string.IsNullOrEmpty(storey.MemoryStoreyId))
                {
                    var memoryStorey = prjEntityFloors[storey.MemoryStoreyId];
                    bimStorey.MemoryStoreyId = storey.MemoryStoreyId;
                    bimStorey.MemoryMatrix3d = storey.MemoryMatrix3d.ToXBimMatrix3D();
                    foreach (var keyValue in memoryStorey.FloorEntityRelations)
                    {
                        var relation = keyValue.Value;
                        if (null == relation)
                            continue;
                        var entityRelation = new THBimElementRelation(CurrentGIndex(), relation.Name);
                        entityRelation.ParentUid = bimStorey.Uid;
                        entityRelation.RelationElementUid = relation.RelationElementUid;
                        entityRelation.RelationElementId = relation.RelationElementId;
                        bimStorey.FloorEntityRelations.Add(entityRelation.Uid, entityRelation);
                    }
                }
                else
                {
                    //多线程有少数据导致后面报错，后续再处理
                    var moveVector = storey.Origin.Point3D2Vector();
                    Parallel.ForEach(storey.Walls, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, wall =>
                    {
                        var wallId = CurrentGIndex();
                        var bimWall = new THBimWall(wallId, string.Format("wall#{0}", wallId), wall.THTCHGeometryParam(), "", wall.Uuid);
                        var wallWidth = wall.Width;
                        bimWall.ParentUid = bimStorey.Uid;
                        var wallRelation = new THBimElementRelation(bimWall.Id, bimWall.Name, bimWall, bimWall.Describe, bimWall.Uid);
                        lock (bimStorey)
                        {
                            bimStorey.FloorEntityRelations.Add(bimWall.Uid, wallRelation);
                            bimStorey.FloorEntitys.Add(bimWall.Uid, bimWall);
                        }
                        if (null != wall.Doors)
                        {
                            foreach (var door in wall.Doors)
                            {
                                var doorId = CurrentGIndex();
                                var bimDoor = new THBimDoor(doorId, string.Format("door#{0}", doorId), door.THTCHGeometryParam(), door.Swing, door.OperationType, "", door.Uuid);
                                bimDoor.ParentUid = bimWall.Uid;
                                var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor, bimDoor.Describe, bimDoor.Uid);
                                doorRelation.ParentUid = storey.Uuid;
                                //添加Opening
                                var doorOpening = DoorWindowOpening(bimDoor.GeometryParam as GeometryStretch, wallWidth, out THBimElementRelation openingRelation);
                                doorOpening.Uid = bimDoor.Uid + bimDoor.ParentUid;
                                doorOpening.ParentUid = bimWall.Uid;
                                openingRelation.Uid = doorOpening.Uid;
                                openingRelation.RelationElementUid = doorOpening.Uid;
                                openingRelation.ParentUid = storey.Uuid;
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimDoor.Uid, doorRelation);
                                    bimStorey.FloorEntitys.Add(bimDoor.Uid, bimDoor);
                                    bimStorey.FloorEntityRelations.Add(openingRelation.Uid, openingRelation);
                                    bimStorey.FloorEntitys.Add(doorOpening.Uid, doorOpening);
                                }
                                lock (allEntitys)
                                {
                                    allEntitys.Add(doorOpening.Uid, doorOpening);
                                    allEntitys.Add(bimDoor.Uid, bimDoor);
                                }
                                bimWall.Openings.Add(doorOpening);
                            }
                        }
                        if (null != wall.Windows)
                        {
                            foreach (var window in wall.Windows)
                            {
                                var windowId = CurrentGIndex();
                                var bimWindow = new THBimWindow(windowId, string.Format("door#{0}", windowId), window.THTCHGeometryParam(), window.WindowType, "", window.Uuid);
                                bimWindow.ParentUid = bimWall.Uid;
                                var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name, bimWindow, bimWindow.Describe, bimWindow.Uid);
                                windowRelation.ParentUid = storey.Uuid;
                                //添加Opening
                                var winOpening = DoorWindowOpening(bimWindow.GeometryParam as GeometryStretch, wallWidth, out THBimElementRelation openingRelation);
                                winOpening.Uid = bimWindow.Uid + bimWindow.ParentUid;
                                winOpening.ParentUid = bimWall.Uid;
                                openingRelation.Uid = winOpening.Uid;
                                openingRelation.RelationElementUid = winOpening.Uid;
                                openingRelation.ParentUid = storey.Uuid;
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimWindow.Uid, windowRelation);
                                    bimStorey.FloorEntitys.Add(bimWindow.Uid, bimWindow);
                                    bimStorey.FloorEntityRelations.Add(winOpening.Uid, openingRelation);
                                    bimStorey.FloorEntitys.Add(winOpening.Uid, winOpening);
                                }
                                lock (allEntitys)
                                {
                                    allEntitys.Add(winOpening.Uid, winOpening);
                                    allEntitys.Add(bimWindow.Uid, bimWindow);
                                }
                                bimWall.Openings.Add(winOpening);
                            }
                        }
                        if (null != wall.Openings)
                        {
                            foreach (var opening in wall.Openings)
                            {
                                var openingId = CurrentGIndex();
                                var bimOpening = new THBimOpening(openingId, string.Format("opening#{0}", openingId), opening.THTCHGeometryParam(), "", opening.Uuid);
                                bimOpening.ParentUid = bimWall.Uid;
                                var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
                                openingRelation.ParentUid = storey.Uuid;
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimOpening.Uid, openingRelation);
                                    bimStorey.FloorEntitys.Add(bimOpening.Uid, bimOpening);
                                }
                                lock (bimStorey)
                                {
                                    allEntitys.Add(bimOpening.Uid, bimOpening);
                                }
                                bimWall.Openings.Add(bimOpening);
                            }
                        }
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimWall.Uid, bimWall);
                        }
                    });
                    Parallel.ForEach(storey.Slabs, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, slab =>
                    {
                        var geoSlab = slab.SlabGeometryParam(out List<GeometryStretch> slabDescendingData);
                        var slabId = CurrentGIndex();
                        var bimSlab = new THBimSlab(slabId, string.Format("slab#{0}", slabId), geoSlab, "", slab.Uuid);
                        bimSlab.ParentUid = bimStorey.Uid;
                        foreach (var item in slabDescendingData)
                            bimSlab.SlabDescendingDatas.Add(item);
                        var slabRelation = new THBimElementRelation(bimSlab.Id, bimSlab.Name, bimSlab, bimSlab.Describe, bimSlab.Uid);
                        bimStorey.FloorEntityRelations.Add(bimSlab.Uid, slabRelation);
                        bimStorey.FloorEntitys.Add(bimSlab.Uid, bimSlab);
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimSlab.Uid, bimSlab);
                        }
                    });
                    Parallel.ForEach(storey.Railings, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, railing =>
                    {
                        var railingGeo = railing.THTCHGeometryParam() as GeometryStretch;
                        if (railingGeo.Outline.Points != null)
                            railingGeo.Outline = railing.Outline.BufferFlatPL(railingGeo.YAxisLength / 2);
                        var railingId = CurrentGIndex();
                        var bimRailing = new THBimRailing(railingId, string.Format("railing#{0}", railingId), railingGeo, "", railing.Uuid);
                        bimRailing.ParentUid = bimStorey.Uid;
                        var railingRelation = new THBimElementRelation(bimRailing.Id, bimRailing.Name, bimRailing, bimRailing.Describe, bimRailing.Uid);
                        bimStorey.FloorEntityRelations.Add(bimRailing.Uid, railingRelation);
                        bimStorey.FloorEntitys.Add(bimRailing.Uid, bimRailing);
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimRailing.Uid, bimRailing);
                        }
                    });
                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                }
                allStoreys.Add(bimStorey.Uid, bimStorey);
                bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
            }
            bimSite.SiteBuildings.Add(bimBuilding.Uid, bimBuilding);
            bimProject.ProjectSite = bimSite;
        }
        private THBimOpening DoorWindowOpening(GeometryStretch doorWindowParam, double wallWidth, out THBimElementRelation openingRelation)
        {
            var cloneParam = doorWindowParam.Clone() as GeometryStretch;
            var openingId = CurrentGIndex();
            if (wallWidth > cloneParam.YAxisLength)
            {
                cloneParam.YAxisLength = wallWidth + 120;
            }
            var bimOpening = new THBimOpening(openingId, string.Format("opening#{0}", openingId), cloneParam);
            openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
            return bimOpening;
        }
    }
}
