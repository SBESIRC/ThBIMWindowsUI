using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc;
using Xbim.Ifc2x3;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometricConstraintResource;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.OverlayNG;

using ThBIMServer.NTS;
using THBimEngine.Domain;
using XbimXplorer.Deduct.Model;

namespace XbimXplorer.Deduct
{
    /// <summary>
    /// 2022/12/14
    /// 临时的测试装修的代码
    /// 后期会和主流程合并
    /// </summary>
    public class FurnishEngine
    {
        public IfcStore IfcArchi;
        private Dictionary<string, DeductGFCModel> ModelList;//key：uid value：model

        /// <summary>
        /// 计算装修逻辑
        /// </summary>
        public void CalculateFurnish()
        {
            ModelList = new Dictionary<string, DeductGFCModel>();
            BuildArchi2D();

            //var archiWalls = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiWall && x.Value.ChildItems.Count() > 0);
            //var archiRooms = ModelList.Where(x => x.Value.ItemType == DeductType.Room);
            var archiStoreys = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey);

            //foreach (var storeyModel in archiStoreys)
            //{
            var storeyModel = archiStoreys.First();
            var elementModelIDs = storeyModel.Value.ChildItems;
            var wallModelIDs = elementModelIDs.Where(o => ModelList[o].ItemType == DeductType.ArchiWall);
            var roomModelIDs = elementModelIDs.Where(o => ModelList[o].ItemType == DeductType.Room);
            var wall2DSence = new ThNTSSpatialIndex(wallModelIDs.Select(o => ModelList[o].Outline).OfType<NetTopologySuite.Geometries.Geometry>().ToList());
            //var room2DSence = new ThNTSSpatialIndex(roomModelIDs.Select(o => ModelList[o].Outline).OfType<NetTopologySuite.Geometries.Geometry>().ToList());
            foreach (var roomID in roomModelIDs)
            {
                var model = ModelList[roomID];
                var a = model.Outline.Shell.Coordinates;
                //model.Outline.
                var adjacentWalls = wall2DSence.SelectFence(model.Outline.Buffer(50));
                if(adjacentWalls.Count() < 4)
                {
                    //认为一个房间最少四堵墙
                }
            }
            //}
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


        private static Dictionary<IfcBuildingStorey, ThIFCNTSSpatialIndex> GetSpIdxIfc2(List<IfcBuildingStorey> storeys, out Dictionary<string, Tuple<IfcProfileDef, IfcAxis2Placement>> wallTupleDict)
        {
            var storeyWallDict = new Dictionary<IfcBuildingStorey, ThIFCNTSSpatialIndex>();
            wallTupleDict = new Dictionary<string, Tuple<IfcProfileDef, IfcAxis2Placement>>();
            foreach (IfcBuildingStorey BuildingStorey in storeys)
            {
                var struProfileInfos = new List<Tuple<IfcProfileDef, IfcAxis2Placement>>();
                foreach (var containElement in BuildingStorey.ContainsElements)
                {
                    var walls = containElement.RelatedElements.OfType<IfcWall>().ToList();
                    foreach (var item in walls)
                    {
                        if (item.Representation.Representations[0].Items[0] is IfcSweptAreaSolid)
                        {
                            var profile = ((IfcSweptAreaSolid)item.Representation.Representations[0].Items[0]).SweptArea;
                            var placement = ((IfcLocalPlacement)item.ObjectPlacement).RelativePlacement;
                            var wTuple = Tuple.Create(profile, placement);
                            struProfileInfos.Add(wTuple);
                            wallTupleDict.Add(item.GlobalId, wTuple);
                        }
                        else
                        {
                            var area = item.Representation.Representations[0].Items[0];
                        }

                    }
                }
                var spIdx = new ThIFCNTSSpatialIndex(struProfileInfos);
                storeyWallDict.Add(BuildingStorey, spIdx);
            }

            return storeyWallDict;
        }


        private static Dictionary<IfcBuildingStorey, ThIFCNTSSpatialIndex> GetSpIdxIfc23(List<IfcBuildingStorey> storeys, out Dictionary<string, Tuple<IfcProfileDef, IfcAxis2Placement>> wallTupleDict)
        {
            var storeyWallDict = new Dictionary<IfcBuildingStorey, ThIFCNTSSpatialIndex>();
            wallTupleDict = new Dictionary<string, Tuple<IfcProfileDef, IfcAxis2Placement>>();
            foreach (IfcBuildingStorey BuildingStorey in storeys)
            {
                var struProfileInfos = new List<Tuple<IfcProfileDef, IfcAxis2Placement>>();
                foreach (var containElement in BuildingStorey.ContainsElements)
                {
                    var walls = containElement.RelatedElements.OfType<IfcWall>().ToList();
                    foreach (var item in walls)
                    {
                        if (item.Representation.Representations[0].Items[0] is IfcSweptAreaSolid)
                        {
                            var profile = ((IfcSweptAreaSolid)item.Representation.Representations[0].Items[0]).SweptArea;
                            var placement = ((IfcLocalPlacement)item.ObjectPlacement).RelativePlacement;
                            var wTuple = Tuple.Create(profile, placement);
                            struProfileInfos.Add(wTuple);
                            wallTupleDict.Add(item.GlobalId, wTuple);
                        }
                        else
                        {
                            var area = item.Representation.Representations[0].Items[0];
                        }

                    }
                }
                var spIdx = new ThIFCNTSSpatialIndex(struProfileInfos);
                storeyWallDict.Add(BuildingStorey, spIdx);
            }

            return storeyWallDict;
        }




        private Dictionary<string, Dictionary<THBimWall, List<Tuple<IfcProfileDef, IfcAxis2Placement>>>> DeductWall(List<IfcBuildingStorey> strucStoreys, Dictionary<IfcBuildingStorey, ThIFCNTSSpatialIndex> storeyWallDict)
        {
            var storeyDict = new Dictionary<string, Dictionary<THBimWall, List<Tuple<IfcProfileDef, IfcAxis2Placement>>>>();




            //foreach (var building in IfcArchi.ProjectSite.SiteBuildings)
            //{
            //    foreach (var storey in building.Value.BuildingStoreys)
            //    {
            //        var wChange = new Dictionary<THBimWall, List<Tuple<IfcProfileDef, IfcAxis2Placement>>>();
            //        storeyDict.Add(storey.Key, wChange);

            //        var storeyItem = storey.Value;
            //        if (storeyItem.MemoryStoreyId == string.Empty || storeyItem.MemoryStoreyId == "")
            //        {
            //            //标准层第一层或非标层
            //            var sStorey = strucStoreys.Where(x =>
            //            {
            //                double elevation = x.Elevation.Value;
            //                if (Math.Abs(elevation - storeyItem.Elevation) <= 50)
            //                    return true;
            //                else
            //                    return false;
            //            }).FirstOrDefault();

            //            if (sStorey == null)
            //            {
            //                continue;
            //            }
            //            storeyWallDict.TryGetValue(sStorey, out var spIdx);

            //            if (spIdx == null)
            //            {
            //                continue;
            //            }
            //            foreach (var w in storeyItem.FloorEntitys)
            //            {
            //                if (w.Value is THBimWall wallItem)
            //                {
            //                    var selectItem = spIdx.SelectCrossingPolygon(wallItem.GeometryParam as GeometryStretch);
            //                    if (selectItem.Count > 0)
            //                    {
            //                        wChange.Add(wallItem, selectItem);
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}

            return storeyDict;

        }



        /// <summary>
        /// key：原墙uid
        /// value ：item1 是否只删除原墙， item2代替的新墙
        /// 如果返回值count=0  => onlyDelete = true 只删除墙， onlyDelete = false 则保留原墙
        /// 返回值count>0 -> 删除原墙，用新墙代替
        /// </summary>
        /// <param name="deductWall"></param>
        /// <returns></returns>
        private static Dictionary<string, Tuple<bool, List<THBimWall>>> CutBimWall(Dictionary<string, Dictionary<THBimWall, List<Tuple<IfcProfileDef, IfcAxis2Placement>>>> deductWall)
        {
            ////////////////////////
            //var item = deductWall.ElementAt(0).Value.ElementAt(2);
            //CutBimWallProcess(item);
            ////////////////////////

            var wallCutDict = new Dictionary<string, Tuple<bool, List<THBimWall>>>();
            foreach (var storey in deductWall)
            {
                var wallCutPairs = storey.Value;
                foreach (var wallCutPair in wallCutPairs)
                {
                    var geomArchi = ((GeometryStretch)wallCutPair.Key.GeometryParam).ToNTSPolygon();
                    var geomStructList = wallCutPair.Value.Select(x => x.Item1.ToNTSPolygon(x.Item2)).ToList();

                    var newWall = DeductService.CutBimWallGeom(geomArchi, geomStructList, out var onlyDelete);

                    var newBimWall = new List<THBimWall>();
                    if (onlyDelete == false)
                    {
                        newBimWall.AddRange(DeductService.ToThBimWall(wallCutPair.Key, newWall));
                    }

                    wallCutDict.Add(wallCutPair.Key.Uid, new Tuple<bool, List<THBimWall>>(onlyDelete, newBimWall));
                }
            }

            return wallCutDict;
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
