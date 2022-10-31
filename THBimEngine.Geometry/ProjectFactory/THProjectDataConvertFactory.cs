﻿using Microsoft.SqlServer.Server;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using THBimEngine.Domain;
using THBimEngine.Domain.Grid;
using THBimEngine.Domain.MidModel;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;

namespace THBimEngine.Geometry.ProjectFactory
{
    public class THProjectDataConvertFactory : ConvertFactoryBase
    {
        public THProjectDataConvertFactory(IfcSchemaVersion ifcSchemaVersion) : base(ifcSchemaVersion)
        {
        }
        public override ConvertResult ProjectConvert(object objProject, bool createSolidMesh)
        {
            var project = objProject as ThTCHProjectData;
            if (null == project)
                throw new System.NotSupportedException();
            ConvertResult convertResult = null;
            //step1 转换几何数据
            ThTCHProjectToTHBimProject(project);
            bimProject.ProjectIdentity = project.Root.GlobalId;
            if (createSolidMesh)
            {
                CreateSolidMesh(allEntitys.Values.ToList());
            }
            //var gridLines = new List<GridLine>();
            //var gridCircles = new List<GridCircle>();
            //var gridTexts = new List<GridText>();
            foreach (var item in allStoreys)
            {
                bimProject.PrjAllStoreys.Add(item.Key, item.Value);
                //var gridSystem = item.Value.GridLineSyetemData;

                //if(gridSystem != null)
                //{
                //    var elevation = item.Value.Elevation;
                //    string json = JsonConvert.SerializeObject(gridSystem, Newtonsoft.Json.Formatting.Indented);
                //    string fileName = Path.Combine(System.IO.Path.GetTempPath(), "GridData.json");
                //    File.WriteAllText(fileName, json);

                //    foreach (var gridLine in gridSystem.GridLines)
                //    {
                //        gridLines.Add(new GridLine(gridLine,1,elevation));
                //    }
                //    foreach (var gridCircleGroup in gridSystem.CircleLableGroups)
                //    {
                //        foreach (var circleLable in gridCircleGroup.CircleLables)
                //        {
                //            gridLines.Add(new GridLine(circleLable,elevation));
                //            gridCircles.Add(new GridCircle(circleLable, elevation));
                //            gridTexts.Add(new GridText(circleLable, elevation));
                //        }
                //    }
                //    foreach (var gridDimensionGroup in gridSystem.DimensionGroups)
                //    {
                //        foreach (var gridDimension in gridDimensionGroup.Dimensions)
                //        {
                //            var dimLines = gridDimension.DimLines;
                //            var mark = gridDimension.Mark;

                //            for (int i =0; i < dimLines.Count;i++)
                //            {
                //                var dimLine = dimLines[i];
                //                gridLines.Add(new GridLine(dimLine, elevation));
                //                if (i == dimLines.Count - 1)
                //                    gridTexts.Add(new GridText(dimLine, mark, elevation));
                //            }
                //        }
                //    }
                //}
                //if(gridSystem != null)
                //{
                //    var bimGrids = new List<THBimGrid>();
                //    foreach (var gridLine in gridSystem.GridLines)
                //    {
                //        bimGrids.Add(new THBimGrid(gridLine));
                //    }
                //    foreach (var gridCircleGroup in gridSystem.CircleLableGroups)
                //    {
                //        foreach (var gridCircle in gridCircleGroup.CircleLables)
                //        {
                //            bimGrids.Add(new THBimGrid(gridCircle));
                //        }
                //    }
                //    foreach (var gridDimensionGroup in gridSystem.DimensionGroups)
                //    {
                //        foreach (var gridDimension in gridDimensionGroup.Dimensions)
                //        {
                //            bimGrids.Add(new THBimGrid(gridDimension));
                //        }
                //    }
                //}

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
            ;
            //convertResult.BimProject.GridLines = gridLines;
            //convertResult.BimProject.GridCircles = gridCircles;
            //convertResult.BimProject.GridTexts = gridTexts;
            return convertResult;
        }
        private void ThTCHProjectToTHBimProject(ThTCHProjectData project)
        {
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project)
                return;
            bimProject = new THBimProject(CurrentGIndex(), project.Root.Name, "", project.Root.GlobalId);
            bimProject.SourceName = "CAD";
            bimProject.ProjectIdentity = project.Root.GlobalId;
            var bimSite = new THBimSite(CurrentGIndex(), "", "", project.Site.Root.GlobalId);
            var building = project.Site.Buildings.First();
            var bimBuilding = new THBimBuilding(CurrentGIndex(), building.Root.Name, "", building.Root.GlobalId);
            foreach (var storey in building.Storeys)
            {
                var bimStorey = new THBimStorey(CurrentGIndex(), storey.Number, storey.Elevation, storey.Height, "", storey.BuildElement.Root.GlobalId);
                bimStorey.Matrix3D = XbimMatrix3D.CreateTranslation(storey.Origin.Point3D2Vector());
                bimStorey.GridLineSyetemData = storey.GridLineSystem;
                var gridSystem = storey.GridLineSystem;
                if (!string.IsNullOrEmpty(storey.MemoryStoreyId))
                {
                    var memoryStorey = prjEntityFloors[storey.MemoryStoreyId];
                    bimStorey.MemoryStoreyId = storey.MemoryStoreyId;
                    bimStorey.MemoryMatrix3d = storey.MemoryMatrix3D.ToXBimMatrix3D();
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
                    if (null == gridSystem)
                    {
                        var tempStorey = building.Storeys.Where(c => c.BuildElement.Root.GlobalId == memoryStorey.Uid).FirstOrDefault();
                        if (null != tempStorey)
                            gridSystem = tempStorey.GridLineSystem;
                    }
                    StoreyAddGridEntity(gridSystem, bimStorey);
                }
                else
                {
                    //多线程有少数据导致后面报错，后续再处理
                    var moveVector = storey.Origin.Point3D2Vector();
                    Parallel.ForEach(storey.Walls, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, wall =>
                    {
                        var wallId = CurrentGIndex();
                        var bimWall = new THBimWall(wallId, string.Format("wall#{0}", wallId), wall.BuildElement.EnumMaterial, wall.BuildElement.THTCHGeometryParam(), "", wall.BuildElement.Root.GlobalId);
                        var wallWidth = wall.BuildElement.Width;
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
                                var bimDoor = new THBimDoor(doorId, string.Format("door#{0}", doorId), door.BuildElement.EnumMaterial, door.BuildElement.THTCHGeometryParam(), door.Swing, door.Operation, "", door.BuildElement.Root.GlobalId);
                                bimDoor.ParentUid = bimWall.Uid;
                                var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor, bimDoor.Describe, bimDoor.Uid);
                                doorRelation.ParentUid = storey.BuildElement.Root.GlobalId;
                                //添加Opening
                                var doorOpening = DoorWindowOpening(bimDoor.GeometryParam as GeometryStretch, wallWidth, out THBimElementRelation openingRelation);
                                doorOpening.Uid = bimDoor.Uid + bimDoor.ParentUid;
                                doorOpening.ParentUid = bimWall.Uid;
                                openingRelation.Uid = doorOpening.Uid;
                                openingRelation.RelationElementUid = doorOpening.Uid;
                                openingRelation.ParentUid = storey.BuildElement.Root.GlobalId;
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
                                var bimWindow = new THBimWindow(windowId, string.Format("door#{0}", windowId), "", window.BuildElement.THTCHGeometryParam(), window.Type, "", window.BuildElement.Root.GlobalId);
                                bimWindow.ParentUid = bimWall.Uid;
                                var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name, bimWindow, bimWindow.Describe, bimWindow.Uid);
                                windowRelation.ParentUid = storey.BuildElement.Root.GlobalId;
                                //添加Opening
                                var winOpening = DoorWindowOpening(bimWindow.GeometryParam as GeometryStretch, wallWidth, out THBimElementRelation openingRelation);
                                winOpening.Uid = bimWindow.Uid + bimWindow.ParentUid;
                                winOpening.ParentUid = bimWall.Uid;
                                openingRelation.Uid = winOpening.Uid;
                                openingRelation.RelationElementUid = winOpening.Uid;
                                openingRelation.ParentUid = storey.BuildElement.Root.GlobalId;
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
                                var bimOpening = new THBimOpening(openingId, string.Format("opening#{0}", openingId), "", opening.BuildElement.THTCHGeometryParam(), "", opening.BuildElement.Root.GlobalId);
                                bimOpening.ParentUid = bimWall.Uid;
                                var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
                                openingRelation.ParentUid = storey.BuildElement.Root.GlobalId;
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
                        var bimSlab = new THBimSlab(slabId, string.Format("slab#{0}", slabId), "", geoSlab, "", slab.BuildElement.Root.GlobalId);
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
                        var railingGeo = railing.BuildElement.THTCHGeometryParam() as GeometryStretch;
                        var railingId = CurrentGIndex();
                        var bimRailing = new THBimRailing(railingId, string.Format("railing#{0}", railingId), railing.BuildElement.EnumMaterial, railingGeo, "", railing.BuildElement.Root.GlobalId);
                        bimRailing.ParentUid = bimStorey.Uid;
                        var railingRelation = new THBimElementRelation(bimRailing.Id, bimRailing.Name, bimRailing, bimRailing.Describe, bimRailing.Uid);
                        bimStorey.FloorEntityRelations.Add(bimRailing.Uid, railingRelation);
                        bimStorey.FloorEntitys.Add(bimRailing.Uid, bimRailing);
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimRailing.Uid, bimRailing);
                        }
                    });
                    StoreyAddGridEntity(gridSystem, bimStorey);
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
            var bimOpening = new THBimOpening(openingId, string.Format("opening#{0}", openingId), "", cloneParam);
            openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
            return bimOpening;
        }
        private void StoreyAddGridEntity(ThGridLineSyetemData gridSystem, THBimStorey bimStorey) 
        {
            if (gridSystem != null)
            {
                if (gridSystem.GridLines != null)
                {
                    foreach (var gridLine in gridSystem.GridLines)
                    {
                        var bimGridLine = new GridLine(gridLine, 1, bimStorey.Elevation);
                        bimGridLine.ParentUid = bimStorey.Uid;
                        var gridLineRelation = new THBimElementRelation(bimGridLine.Id, bimGridLine.Name, bimGridLine, bimGridLine.Describe, bimGridLine.Uid);
                        bimStorey.FloorEntityRelations.Add(bimGridLine.Uid, gridLineRelation);
                        bimStorey.FloorEntitys.Add(bimGridLine.Uid, bimGridLine);
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimGridLine.Uid, bimGridLine);
                        }
                    }
                }
                if (gridSystem.CircleLableGroups != null)
                {
                    foreach (var gridCircleGroup in gridSystem.CircleLableGroups)
                    {
                        foreach (var circleLable in gridCircleGroup.CircleLables)
                        {
                            var bimGridLine = new GridLine(circleLable, bimStorey.Elevation);
                            bimGridLine.ParentUid = bimStorey.Uid;
                            var gridLineRelation = new THBimElementRelation(bimGridLine.Id, bimGridLine.Name, bimGridLine, bimGridLine.Describe, bimGridLine.Uid);
                            bimStorey.FloorEntityRelations.Add(bimGridLine.Uid, gridLineRelation);
                            bimStorey.FloorEntitys.Add(bimGridLine.Uid, bimGridLine);
                            lock (allEntitys)
                            {
                                allEntitys.Add(bimGridLine.Uid, bimGridLine);
                            }


                            var bimGridCircle = new GridCircle(circleLable, bimStorey.Elevation);
                            bimGridCircle.ParentUid = bimStorey.Uid;
                            var gridCircleRelation = new THBimElementRelation(bimGridCircle.Id, bimGridCircle.Name, bimGridCircle, bimGridCircle.Describe, bimGridCircle.Uid);
                            bimStorey.FloorEntityRelations.Add(bimGridCircle.Uid, gridCircleRelation);
                            bimStorey.FloorEntitys.Add(bimGridCircle.Uid, bimGridCircle);
                            lock (allEntitys)
                            {
                                allEntitys.Add(bimGridCircle.Uid, bimGridCircle);
                            }

                            var bimGridText = new GridText(circleLable, bimStorey.Elevation);
                            bimGridText.ParentUid = bimStorey.Uid;
                            var gridTextRelation = new THBimElementRelation(bimGridText.Id, bimGridText.Name, bimGridText, bimGridText.Describe, bimGridText.Uid);
                            bimStorey.FloorEntityRelations.Add(bimGridText.Uid, gridTextRelation);
                            bimStorey.FloorEntitys.Add(bimGridText.Uid, bimGridText);
                            lock (allEntitys)
                            {
                                allEntitys.Add(bimGridText.Uid, bimGridText);
                            }
                        }
                    }
                }
                if (gridSystem.DimensionGroups != null)
                {
                    foreach (var gridDimensionGroup in gridSystem.DimensionGroups)
                    {
                        foreach (var gridDimension in gridDimensionGroup.Dimensions)
                        {
                            var dimLines = gridDimension.DimLines;
                            var mark = gridDimension.Mark;

                            for (int i = 0; i < dimLines.Count; i++)
                            {
                                var dimLine = dimLines[i];
                                var bimGridLine = new GridLine(dimLine, bimStorey.Elevation);
                                bimGridLine.ParentUid = bimStorey.Uid;
                                var gridLineRelation = new THBimElementRelation(bimGridLine.Id, bimGridLine.Name, bimGridLine, bimGridLine.Describe, bimGridLine.Uid);
                                bimStorey.FloorEntityRelations.Add(bimGridLine.Uid, gridLineRelation);
                                bimStorey.FloorEntitys.Add(bimGridLine.Uid, bimGridLine);
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimGridLine.Uid, bimGridLine);
                                }


                                if (i == dimLines.Count - 1)
                                {
                                    var bimGridText = new GridText(dimLine, mark, bimStorey.Elevation);
                                    bimGridText.ParentUid = bimStorey.Uid;
                                    var gridTextRelation = new THBimElementRelation(bimGridText.Id, bimGridText.Name, bimGridText, bimGridText.Describe, bimGridText.Uid);
                                    bimStorey.FloorEntityRelations.Add(bimGridText.Uid, gridTextRelation);
                                    bimStorey.FloorEntitys.Add(bimGridText.Uid, bimGridText);
                                    lock (allEntitys)
                                    {
                                        allEntitys.Add(bimGridText.Uid, bimGridText);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
