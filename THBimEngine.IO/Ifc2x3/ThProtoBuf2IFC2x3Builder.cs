﻿using System;
using System.IO;
using System.Collections.Generic;

using Xbim.IO;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using System.Linq;

namespace ThBIMServer.Ifc2x3
{
    public class ThProtoBuf2IFC2x3Builder
    {
        public static void BuildIfcModel(IfcStore model, ThTCHProjectData project)
        {
            if (model != null)
            {
                var storeys = new List<IfcBuildingStorey>();
                var site = ThProtoBuf2IFC2x3Factory.CreateSite(model);
                var building = ThProtoBuf2IFC2x3Factory.CreateBuilding(model, site, project.Sites[0].Buildings[0]);
                foreach (var thtchstorey in project.Sites[0].Buildings[0].Storeys)
                {
                    var storeyData = thtchstorey;
                    var walls = new List<IfcWall>();
                    var columns = new List<IfcColumn>();
                    var beams = new List<IfcBeam>();
                    var slabs = new List<IfcSlab>();
                    var doors = new List<IfcDoor>();
                    var windows = new List<IfcWindow>();
                    var railings = new List<IfcRailing>();
                    var rooms = new List<IfcSpace>();
                    var storey = ThProtoBuf2IFC2x3Factory.CreateStorey(model, building, thtchstorey);
                    if(!string.IsNullOrEmpty(thtchstorey.MemoryStoreyId))
                    {
                        storeyData = project.Sites[0].Buildings[0].Storeys.FirstOrDefault(o => o.BuildElement.Root.GlobalId == thtchstorey.MemoryStoreyId);
                    }
                    storeys.Add(storey);
                    foreach (var thtchwall in storeyData.Walls)
                    {
                        var wall = ThProtoBuf2IFC2x3Factory.CreateWall(model, storey, thtchwall);
                        walls.Add(wall);
                        foreach (var thtchdoor in thtchwall.Doors)
                        {
                            doors.Add(SetupDoor(model, storey, wall, thtchwall, thtchdoor));
                        }
                        foreach (var thtchwindow in thtchwall.Windows)
                        {
                            windows.Add(SetupWindow(model, storey, wall, thtchwall, thtchwindow));
                        }
                        foreach (var thtchhole in thtchwall.Openings)
                        {
                            SetupHole(model, storey, wall, thtchhole);
                        }
                    }
                    //暂不支持梁和柱
                    //foreach (var thtchcolumn in thtchstorey.Columns)
                    //{
                    //    var column = ThProtoBuf2IFC2x3Factory.CreateColumn(Model, thtchcolumn, floor_origin);
                    //    columns.Add(column);
                    //}
                    //foreach (var thtchbeam in thtchstorey.Beams)
                    //{
                    //    var beam = ThProtoBuf2IFC2x3Factory.CreateBeam(Model, thtchbeam, floor_origin);
                    //    beams.Add(beam);
                    //}
                    foreach (var thtchslab in storeyData.Slabs)
                    {
                        var slab = ThProtoBuf2IFC2x3Factory.CreateBrepSlab(model, storey, thtchslab);
                        slabs.Add(slab);
                    }
                    foreach (var thtchrailing in storeyData.Railings)
                    {
                        var railing = ThProtoBuf2IFC2x3Factory.CreateRailing(model, storey, thtchrailing);
                        railings.Add(railing);
                    }
                    foreach (var thtchRoom in storeyData.Rooms)
                    {
                        var room = ThProtoBuf2IFC2x3Factory.CreateRoom(model, storey, thtchRoom);
                        rooms.Add(room);
                    }

                    // IIfcRelContainedInSpatialStructure 关系
                    ThProtoBuf2IFC2x3Factory.RelContainSlabs2Storey(model, slabs, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainWalls2Storey(model, walls, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainColumns2Storey(model, columns, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainBeams2Storey(model, beams, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainDoors2Storey(model, doors, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainWindows2Storey(model, windows, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainsRailings2Storey(model, railings, storey);
                    ThProtoBuf2IFC2x3Factory.RelContainsRooms2Storey(model, rooms, storey);
                }

                // IfcRelAggregates 关系
                ThProtoBuf2IFC2x3RelAggregatesFactory.Create(model, building, storeys);
            }
        }

        public static void BuildIfcModel(IfcStore model, ThSUProjectData project)
        {
            if (model != null)
            {
                var storeys = new List<IfcBuildingStorey>();
                // 虚拟set
                var site = ThProtoBuf2IFC2x3Factory.CreateSite(model);
                //var building = ThProtoBuf2IFC2x3Factory.CreateBuilding(model, site, project.Building.Root.Name);
                var building = ThProtoBuf2IFC2x3Factory.CreateBuilding(model, site, project.Root.Name + "Building");
                var definitions = project.Definitions;
                foreach (var storey in project.Building.Storeys)
                {
                    if (storey.Buildings.Count > 0)
                    {
                        var ifcStorey = ThProtoBuf2IFC2x3Factory.CreateStorey(model, building, storey.Number.ToString());
                        storeys.Add(ifcStorey);
                        var suElements = new List<IfcBuildingElement>();
                        foreach (var element in storey.Buildings)
                        {
                            var def = definitions[element.Component.DefinitionIndex];
                            IfcBuildingElement ifcBuildingElement;
                            ifcBuildingElement = ThProtoBuf2IFC2x3Factory.CreatedSUElement(model, def, element.Component);
                            suElements.Add(ifcBuildingElement);
                        }
                        ThProtoBuf2IFC2x3Factory.RelContainsSUElements2Storey(model, suElements, ifcStorey);
                    }
                }
                // IfcRelAggregates 关系
                ThProtoBuf2IFC2x3RelAggregatesFactory.Create(model, building, storeys);
            }
        }

        public static IfcDoor SetupDoor(IfcStore model, IfcBuildingStorey storey, IfcWall ifcWall, ThTCHWallData wall, ThTCHDoorData door)
        {
            var ifcDoor = ThProtoBuf2IFC2x3Factory.CreateDoor(model, storey, door);
            var ifcHole = ThProtoBuf2IFC2x3Factory.CreateHole(model, storey, wall, door);
            ThProtoBuf2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcDoor, ifcHole);
            return ifcDoor;
        }

        public static IfcWindow SetupWindow(IfcStore model, IfcBuildingStorey storey, IfcWall ifcWall, ThTCHWallData wall, ThTCHWindowData window)
        {
            var ifcWindow = ThProtoBuf2IFC2x3Factory.CreateWindow(model, storey, window);
            var ifcHole = ThProtoBuf2IFC2x3Factory.CreateHole(model, storey, wall, window);
            ThProtoBuf2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcWindow, ifcHole);
            return ifcWindow;
        }

        public static IfcOpeningElement SetupHole(IfcStore model, IfcBuildingStorey storey, IfcWall ifcWall, ThTCHOpeningData hole)
        {
            var ifcHole = ThProtoBuf2IFC2x3Factory.CreateHole(model, storey, hole);
            ThProtoBuf2IFC2x3Factory.BuildRelationship(model, ifcWall, ifcHole);
            return ifcHole;
        }

        public static void SaveIfcModel(IfcStore model, string filepath)
        {
            if (model != null)
            {
                using (var txn = model.BeginTransaction("save ifc file"))
                {
                    try
                    {
                        model.SaveAs(filepath, IfcStorageType.Ifc);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine("Failed to save HelloWall.ifc");
                        Console.WriteLine(e.Message);
                    }
                    txn.Commit();
                }
            }
        }

        public static void SaveIfcModelByStream(IfcStore model, Stream stream)
        {
            if (model != null)
            {
                using (var txn = model.BeginTransaction("save ifc file"))
                {
                    try
                    {
                        model.SaveAsIfc(stream);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine("Failed to save HelloWall.ifc");
                        Console.WriteLine(e.Message);
                    }
                    txn.Commit();
                }
            }
        }
    }
}