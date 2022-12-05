using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


namespace XbimXplorer.Deduct
{
    internal class DeductEngine23
    {
        //input
        public Xbim.Ifc.IfcStore IfcStore;
        public THBimProject ArchiProject;

        //output
        public void DeductIFC23Engine()
        {
            var project = IfcStore.Instances.FirstOrDefault<IfcProject>();
            var buildlings = project.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as IfcBuilding;
            var struStoreys = buildlings.BuildingStoreys.OfType<IfcBuildingStorey>().ToList();

            var storeyWallDict = GetSpIdxIfc23(struStoreys, out var wallTupleDict);
            var deductWall = DeductWall(struStoreys, storeyWallDict);

            //var debug = new Dictionary<THBimWall, List<string>>();
            //foreach (var s in deductWall)
            //{
            //    foreach (var cutWall in s.Value)
            //    {
            //        var swall = new List<string>();


            //        foreach (var ifc in cutWall.Value)
            //        {
            //            var suid = wallTupleDict.Where(x => x.Value == ifc).FirstOrDefault().Key;
            //            swall.Add(suid);
            //        }

            //        debug.Add(cutWall.Key, swall);
            //    }
            //}
           

            var wallCutResult = CutBimWall(deductWall);


            //var wallnew = wallCutResult.ElementAt(0).Value.Item2;
            //var wallori = debug.ElementAt(0).Key;

            DeductService.UpdateNewWallGeom(ref wallCutResult);

            DeductService.UpdateProject(ref ArchiProject, wallCutResult);

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

            foreach (var building in ArchiProject.ProjectSite.SiteBuildings)
            {
                foreach (var storey in building.Value.BuildingStoreys)
                {
                    var wChange = new Dictionary<THBimWall, List<Tuple<IfcProfileDef, IfcAxis2Placement>>>();
                    storeyDict.Add(storey.Key, wChange);

                    var storeyItem = storey.Value;
                    if (storeyItem.MemoryStoreyId == string.Empty || storeyItem.MemoryStoreyId == "")
                    {
                        //标准层第一层或非标层
                        var sStorey = strucStoreys.Where(x =>
                        {
                            double elevation = x.Elevation.Value;
                            if (Math.Abs(elevation - storeyItem.Elevation) <= 50)
                                return true;
                            else
                                return false;
                        }).FirstOrDefault();

                        if (sStorey == null)
                        {
                            continue;
                        }
                        storeyWallDict.TryGetValue(sStorey, out var spIdx);

                        if (spIdx == null)
                        {
                            continue;
                        }
                        foreach (var w in storeyItem.FloorEntitys)
                        {
                            if (w.Value is THBimWall wallItem)
                            {
                                var selectItem = spIdx.SelectCrossingPolygon(wallItem.GeometryParam as GeometryStretch);
                                if (selectItem.Count > 0)
                                {
                                    wChange.Add(wallItem, selectItem);
                                }
                            }
                        }
                    }
                }
            }

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

    }
}
