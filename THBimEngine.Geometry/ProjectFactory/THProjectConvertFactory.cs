using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Domain.Model;
using THBimEngine.Geometry.NTS;
using Xbim.Common.Step21;

namespace THBimEngine.Geometry.ProjectFactory
{
    public class THProjectConvertFactory: ConvertFactoryBase
    {
        public THProjectConvertFactory(IfcSchemaVersion ifcSchemaVersion):base(ifcSchemaVersion)
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
            if (createSolidMesh)
            {
                CreateSolidMesh(allEntitys);
            }
            var projectEntitys = allEntitys.Where(c => c != null).ToDictionary(c => c.Uid, x => x);
            convertResult = new ConvertResult(bimProject, allStoreys, projectEntitys);
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
                        bimStorey.FloorEntityRelations.Add(entityRelation.Uid,entityRelation);
                    }
                }
                else 
                {
                    //多线程有少数据导致后面报错，后续再处理
                    Parallel.ForEach(storey.Walls, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, wall =>
                    {
                        var wallId = CurrentGIndex();
                        var bimWall = new THBimWall(wallId, string.Format("wall#{0}", wallId), wall.THTCHGeometryParam(), "", wall.Uuid);
                        bimWall.ParentUid = bimStorey.Uid;
                        var wallRelation = new THBimElementRelation(bimWall.Id, bimWall.Name,bimWall, bimWall.Describe, bimWall.Uid);
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
                                var bimDoor = new THBimDoor(doorId, string.Format("door#{0}", doorId), door.THTCHGeometryParam(), "", door.Uuid);
                                bimDoor.ParentUid = bimWall.Uid;
                                var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor,bimDoor.Describe, bimDoor.Uid);
                                doorRelation.ParentUid = storey.Uuid;
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimDoor.Uid, doorRelation);
                                    bimStorey.FloorEntitys.Add(bimDoor.Uid, bimDoor);
                                }
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimDoor);
                                }
                            }
                        }
                        if (null != wall.Windows)
                        {
                            foreach (var window in wall.Windows)
                            {
                                var windowId = CurrentGIndex();
                                var bimWindow = new THBimWindow(windowId, string.Format("window#{0}", windowId), window.THTCHGeometryParam(), "", window.Uuid);
                                bimWindow.ParentUid = bimWall.Uid;
                                var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name,bimWindow, bimWindow.Describe, bimWindow.Uid);
                                windowRelation.ParentUid = storey.Uuid;
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimWindow.Uid, windowRelation);
                                    bimStorey.FloorEntitys.Add(bimWindow.Uid, bimWindow);
                                }
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimWindow);
                                }
                            }
                        }
                        if (null != wall.Openings)
                        {
                            foreach (var opening in wall.Openings)
                            {
                                var opeingId = CurrentGIndex();
                                var bimOpening = new THBimOpening(opeingId, string.Format("opening#{0}", opeingId), opening.THTCHGeometryParam(), "", opening.Uuid);
                                bimOpening.ParentUid = bimWall.Uid;
                                var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name,bimOpening, bimOpening.Describe, bimOpening.Uid);
                                openingRelation.ParentUid = storey.Uuid;
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimOpening.Uid, openingRelation);
                                    bimStorey.FloorEntitys.Add(bimOpening.Uid, bimOpening);
                                }
                                lock (bimStorey)
                                {
                                    allEntitys.Add(bimOpening);
                                }
                                bimWall.Openings.Add(bimOpening);
                            }
                        }
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimWall);
                        }
                    });
                    Parallel.ForEach(storey.Slabs, new ParallelOptions() { MaxDegreeOfParallelism=1}, slab =>
                    {
                        var slabId = CurrentGIndex();
                        var geoSlab = slab.SlabGeometryParam(out List<GeometryStretch> slabDescendingData);
                        var bimSlab = new THBimSlab(slabId, string.Format("slab#{0}", slabId), geoSlab, "", slab.Uuid);
                        bimSlab.ParentUid = bimStorey.Uid;
                        foreach (var item in slabDescendingData)
                            bimSlab.SlabDescendingDatas.Add(item);
                        var slabRelation = new THBimElementRelation(bimSlab.Id, bimSlab.Name, bimSlab, bimSlab.Describe, bimSlab.Uid);
                        bimStorey.FloorEntityRelations.Add(bimSlab.Uid, slabRelation);
                        bimStorey.FloorEntitys.Add(bimSlab.Uid, bimSlab);
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimSlab);
                        }
                    });
                    Parallel.ForEach(storey.Railings, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, railing => 
                    {
                        var railingGeo = railing.THTCHGeometryParam() as GeometryStretch;
                        if(railingGeo.OutLine.Points != null)
                            railingGeo.OutLine = railing.Outline.BufferFlatPL(railingGeo.YAxisLength/2);
                        var railingId = CurrentGIndex();
                        var bimRailing = new THBimRailing(railingId, string.Format("railing#{0}", railingId), railingGeo, "", railing.Uuid);
                        bimRailing.ParentUid = bimStorey.Uid;
                        var railingRelation = new THBimElementRelation(bimRailing.Id, bimRailing.Name, bimRailing, bimRailing.Describe, bimRailing.Uid);
                        bimStorey.FloorEntityRelations.Add(bimRailing.Uid, railingRelation);
                        bimStorey.FloorEntitys.Add(bimRailing.Uid, bimRailing);
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimRailing);
                        }
                    });
                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                }
                allStoreys.Add(bimStorey.Uid, bimStorey);
                bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
            }
            bimSite.SiteBuildings.Add(bimBuilding.Uid,bimBuilding);
            bimProject.ProjectSite = bimSite;
        }
    }
}
