using System;
using System.Linq;
using System.Collections.Generic;

using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using XbimXplorer.Deduct.Model;
using ThBIMServer.NTS;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Geometries;

namespace XbimXplorer.Deduct
{
    internal class Build2DModelService
    {
        //----input
        public IfcStore IfcStruct;
        public IfcStore IfcArchi;

        //----output
        public Dictionary<string, DeductGFCModel> ModelList;//key：uid value：model

        public void Build2DModel()
        {
            ModelList = new Dictionary<string, DeductGFCModel>();
            BuildStruct2D();
            BuildArchi2D();
            FixArchi2D();
        }

        private void BuildStruct2D()
        {
            bool isArchi = false;
            var prjStruct = IfcStruct.Instances.FirstOrDefault<IfcProject>();
            var buildStruct = prjStruct.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as IfcBuilding;
            var storeyStruct = buildStruct.BuildingStoreys.OfType<IfcBuildingStorey>().ToList();

            ////这里暂时只放建筑的，GFC只需要一个building就好了
            //var dmBuilding = ToDeductModel(buildStruct);
            //modelList.Add(dmBuilding.UID, dmBuilding);

            foreach (var ifcStorey in storeyStruct)
            {
                var dmStorey = ToDeductModel(ifcStorey, isArchi);
                ModelList.Add(dmStorey.UID, dmStorey);

                foreach (var containElement in ifcStorey.ContainsElements)
                {
                    var elements = containElement.RelatedElements.OfType<IfcProduct>();
                    var walls = elements.OfType<IfcWall>().ToList();
                    foreach (var w in walls)
                    {
                        var wm = new DeductGFCModel(w, isArchi);
                        ModelList.Add(wm.UID, wm);
                        dmStorey.ChildItems.Add(wm.UID);
                    }

                    //暂定楼板使用结构板
                    var slab = elements.OfType<IfcSlab>().ToList();
                    foreach (var s in slab)
                    {
                        var slabModel = new DeductGFCModel(s, isArchi);
                        ModelList.Add(slabModel.UID, slabModel);
                        dmStorey.ChildItems.Add(slabModel.UID);
                    }
                }
            }
        }

        private void BuildArchi2D()
        {
            bool isArchi = true;
            var prjArchi = IfcArchi.Instances.FirstOrDefault<IfcProject>();
            var buildArchi = prjArchi.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as IfcBuilding;
            var storeyArchi = buildArchi.BuildingStoreys.OfType<IfcBuildingStorey>().ToList();

            var dmBuilding = ToDeductModel(buildArchi, isArchi);
            ModelList.Add(dmBuilding.UID, dmBuilding);

            foreach (var ifcStorey in storeyArchi)
            {
                var dmStorey = ToDeductModel(ifcStorey, isArchi);
                ModelList.Add(dmStorey.UID, dmStorey);
                dmBuilding.ChildItems.Add(dmStorey.UID);

                foreach (var containElement in ifcStorey.ContainsElements)
                {
                    var elements = containElement.RelatedElements.OfType<IfcProduct>();
                    var walls = elements.OfType<IfcWall>().ToList();
                    foreach (var w in walls)
                    {
                        var wm = new DeductGFCModel(w, isArchi);
                        ModelList.Add(wm.UID, wm);
                        dmStorey.ChildItems.Add(wm.UID);
                        var doorWindow = CreateModelWindowDoor(wm);
                        wm.ChildItems.AddRange(doorWindow.Select(x => x.UID));
                        doorWindow.ForEach(x => ModelList.Add(x.UID, x));
                    }

                    var space = elements.OfType<IfcSpace>().ToList();
                    foreach (var s in space)
                    {
                        var roomModel = new DeductGFCModel(s, isArchi);
                        ModelList.Add(roomModel.UID, roomModel);
                        dmStorey.ChildItems.Add(roomModel.UID);
                    }
                }
            }
        }

        private void FixArchi2D()
        {
            var building = ModelList.Where(x => x.Value.ItemType == DeductType.Building).ToList();
            for (int i = 0; i < building.Count; i++)
            {
                var buildingPair = building[i];
                var storeys = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey && buildingPair.Value.ChildItems.Contains(x.Key)).ToList();
                storeys = storeys.OrderBy(x => x.Value.GlobalZ).ToList();
                var tol_angleSA = 1 / 180.0 * Math.PI;
                var tol_Simplify = 1;
                var tol_tooSmallCut = 201;

                for (int j = 0; j < storeys.Count; j++)
                {
                    var storeyPair = storeys[j];
                    var storeyItemList = ModelList.Where(x => storeyPair.Value.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();
                    var archiWallList = storeyItemList.Where(x => x.ItemType == DeductType.ArchiWall).ToList();

                    var geometries = new List<NetTopologySuite.Geometries.Geometry>();
                    archiWallList.ForEach(x => geometries.Add(x.Outline));
                    var spatialIndex = new ThNTSSpatialIndex(geometries);

                    var add = new List<DeductGFCModel>();
                    var delete = new List<DeductGFCModel>();
                    archiWallList.ForEach(wall =>
                    {
                        var geometies = new List<Polygon>();
                        var filter = spatialIndex.SelectCrossingPolygon(wall.Outline);
                        var archiWalls = archiWallList.Where(x => filter.Contains(x.Outline)).Except<DeductGFCModel>(new List<DeductGFCModel> { wall }).Where(x => x.Width - wall.Width > 10.0).Where(x => DeductService.IsParallelWall(x, wall, tol_angleSA)).ToList();
                        if (archiWalls.Count == 0)
                        {
                            return;
                        }
                        var geomArchi = wall.Outline;
                        var geomStructList = archiWalls.Select(x => x.Outline).ToList();

                        var geomStructBufferList = archiWalls.Select(x => DeductService.BufferWall(x, wall, wall.Width, tol_angleSA)).ToList();
                        var geomStructUnion = OverlayNGRobust.Union(geomStructBufferList);

                        if (geomStructUnion.Contains(geomArchi))
                        {
                            delete.Add(wall);
                        }
                        else
                        {
                            var cutArchiWallPolyTemp = new List<Polygon>();
                            var cutArchiWall = geomArchi.Difference(geomStructUnion);
                            if (cutArchiWall is GeometryCollection collect)
                            {
                                cutArchiWallPolyTemp.AddRange(collect.Geometries.OfType<Polygon>().ToList());
                            }
                            else if (cutArchiWall is Polygon cutArchiWallpl)
                            {
                                cutArchiWallPolyTemp.Add(cutArchiWallpl);
                            }

                            var cutArchiWallPoly = new List<Polygon>();
                            cutArchiWallPoly.AddRange(cutArchiWallPolyTemp.SelectMany(x => x.SimplifyPl(tol_Simplify)));

                            var cutArchiWallNotSmall = new List<Polygon>();
                            cutArchiWallNotSmall.AddRange(cutArchiWallPoly);
                            var cutPolyObb = cutArchiWallNotSmall.Where(x => x.Coordinates.Count() > 0).Select(x => x.ToObb()).ToList();

                            var cutPolyObbNotSmall = DeductService.RemoveTooSmallCutWallShortSide(cutPolyObb, tol_tooSmallCut);

                            if (cutPolyObbNotSmall.Count == 0)
                            {
                                delete.Add(wall);
                            }
                            else
                            {
                                geometies.AddRange(cutPolyObbNotSmall);
                                delete.Add(wall);
                            }
                        }

                        if (geometies.Count > 0)
                        {
                            add.AddRange(DeductService.ToWallModel(wall, geometies));
                        }
                    });

                    delete.ForEach(x =>
                    {
                        ModelList.Remove(x.UID);
                        storeyPair.Value.ChildItems.Remove(x.UID);
                    });
                    add.ForEach(x =>
                    {
                        ModelList.Add(x.UID, x);
                        storeyPair.Value.ChildItems.Add(x.UID);
                    });
                }
            }
        }

        private static DeductGFCModel ToDeductModel(IfcBuilding ifc, bool isArchi)
        {
            var dm = new DeductGFCModel();
            dm.IFC = ifc;
            dm.UID = ifc.GlobalId;
            dm.ItemType = DeductGFCModel.GetDeductType(ifc, isArchi);

            return dm;
        }

        private static DeductGFCModel ToDeductModel(IfcBuildingStorey ifc, bool isArchi)
        {
            var dm = new DeductGFCModel();
            dm.IFC = ifc;
            dm.UID = ifc.GlobalId;
            dm.GlobalZ = ifc.Elevation.Value;
            double Storey_Height = double.Parse(((ifc.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
            dm.ZValue = Storey_Height;
            dm.ItemType = DeductGFCModel.GetDeductType(ifc, isArchi);

            return dm;
        }

        private List<DeductGFCModel> CreateModelWindowDoor(DeductGFCModel wm)
        {
            var doorWindow = new List<DeductGFCModel>();

            var relVoidsElement = IfcArchi.Instances.OfType<IfcRelVoidsElement>();
            var wall_relVoidsElements = relVoidsElement.Where(o => o.RelatingBuildingElement == wm.IFC).ToList();
            var opennings = wall_relVoidsElements.Select(x => x.RelatedOpeningElement).ToList();

            var relFillsElement = IfcArchi.Instances.OfType<IfcRelFillsElement>();
            var openning_rel = opennings.SelectMany(o => relFillsElement.Where(x => x.RelatingOpeningElement == o)).ToList();
            var doorWindowIFC = openning_rel.Select(x => x.RelatedBuildingElement).ToList();

            var door = doorWindowIFC.OfType<IfcDoor>().Select(x => new DeductGFCModel(x, true)).ToList();
            var windows = doorWindowIFC.OfType<IfcWindow>().Select(x => new DeductGFCModel(x, true)).ToList();

            doorWindow.AddRange(door);
            doorWindow.AddRange(windows);

            return doorWindow;

        }
    }
}
