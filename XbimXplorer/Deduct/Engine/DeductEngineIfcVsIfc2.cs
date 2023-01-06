using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.OverlayNG;

using ThBIMServer.NTS;
using THBimEngine.Domain;
using XbimXplorer.Deduct.Model;

namespace XbimXplorer.Deduct
{
    internal class DeductEngineIfcVsIfc
    {
        public Dictionary<string, DeductGFCModel> ModelList;//key：uid value：model
        private bool debugMode = false;
        private string debugPrintPath = "D:\\project\\14.ThBim\\chart\\poly{0}.cs";

        public void DeductEngine()
        {
            //2d信息都在model的outline里面，参考如下过滤信息
            var structStorey = ModelList.Where(x => x.Value.ItemType == DeductType.StructStorey).Select(x => x.Value).ToList();
            var archiStorey = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey).Select(x => x.Value).ToList();

            if (structStorey.Count == 0 || archiStorey.Count == 0)
            {
                return;
            }

            var storeyWallDict = GetStructWallSpIdx(structStorey);
            var deductWall = DeductWall(structStorey, archiStorey, storeyWallDict);
            var wallCutResult = CutWall(deductWall);

            UpdateRelationship(archiStorey, wallCutResult);

        }


        private Dictionary<string, Tuple<ThNTSSpatialIndex, Lookup<Polygon, DeductGFCModel>>> GetStructWallSpIdx(List<DeductGFCModel> structStorey)
        {
            var storeyWallDict = new Dictionary<string, Tuple<ThNTSSpatialIndex, Lookup<Polygon, DeductGFCModel>>>();


            foreach (var storey in structStorey)
            {
                var wall = ModelList.Where(x => storey.ChildItems.Contains(x.Key) && x.Value.ItemType == DeductType.StructWall).ToList();

                var polyToWall = (Lookup<Polygon, DeductGFCModel>)wall.ToLookup(x => x.Value.Outline, x => x.Value);

                var wallPoly = wall.Select(x => x.Value.Outline).OfType<NetTopologySuite.Geometries.Geometry>().ToList();

                var idx = new ThNTSSpatialIndex(wallPoly);

                storeyWallDict.Add(storey.UID, new Tuple<ThNTSSpatialIndex, Lookup<Polygon, DeductGFCModel>>(idx, polyToWall));

            }

            return storeyWallDict;

        }

        /// <summary>
        /// archi storey uid , archiwall, conflict structure walls
        /// </summary>
        /// <param name="strucStoreys"></param>
        /// <param name="storeyWallDict"></param>
        /// <returns></returns>
        private Dictionary<string, Dictionary<DeductGFCModel, List<DeductGFCModel>>> DeductWall(List<DeductGFCModel> structStorey, List<DeductGFCModel> archiStorey, Dictionary<string, Tuple<ThNTSSpatialIndex, Lookup<Polygon, DeductGFCModel>>> storeyWallDict)
        {
            var wallConflictDict = new Dictionary<string, Dictionary<DeductGFCModel, List<DeductGFCModel>>>();

            //foreach (var aStorey in archiStorey)
            for (int i = 0; i < archiStorey.Count(); i++)
            {
                var wChange = new Dictionary<DeductGFCModel, List<DeductGFCModel>>();

                var aStorey = archiStorey[i];
                wallConflictDict.Add(aStorey.UID, wChange);

                var sStorey = structStorey.Where(x =>
                        {
                            double elevation = x.GlobalZ;
                            if (Math.Abs(elevation - aStorey.GlobalZ) <= 200)
                                return true;
                            else
                                return false;
                        }).FirstOrDefault();

                if (sStorey == null)
                {
                    continue;
                }

                storeyWallDict.TryGetValue(sStorey.UID, out var spIdxTuple);

                if (spIdxTuple == null)
                {
                    continue;
                }

                foreach (var archiWUID in aStorey.ChildItems)
                {
                    var archiW = ModelList[archiWUID];
                    if (archiW.ItemType == DeductType.ArchiWall)
                    {
                        var selectItem = spIdxTuple.Item1.SelectCrossingPolygon(archiW.Outline);
                        if (selectItem.Count > 0)
                        {
                            var conflictStructWall = new List<DeductGFCModel>();
                            foreach (var item in spIdxTuple.Item2.Where(o => selectItem.Contains(o.Key)))
                            {
                                conflictStructWall.Add(item.First());
                            }
                            wChange.Add(archiW, conflictStructWall);
                        }
                    }
                }
            }

            return wallConflictDict;

        }

        /// <summary>
        /// key：原墙uid
        /// value ：item1 是否只删除原墙， item2代替的新墙
        /// 如果返回值count=0  => onlyDelete = true 只删除墙， onlyDelete = false 则保留原墙
        /// 返回值count>0 -> 删除原墙，用新墙代替
        /// </summary>
        /// <param name="deductWall"></param>
        /// <returns></returns>
        private Dictionary<string, Tuple<bool, List<DeductGFCModel>>> CutWall(Dictionary<string, Dictionary<DeductGFCModel, List<DeductGFCModel>>> deductWall)
        {
            var wallCutDict = new Dictionary<string, Tuple<bool, List<DeductGFCModel>>>();

            for (int n = 0; n < deductWall.Count(); n++)
            {
                var debugPrintPathStorey = string.Format(debugPrintPath, n);
                if (debugMode == true)
                {
                    if (System.IO.File.Exists(debugPrintPathStorey))
                    {
                        System.IO.File.Delete(debugPrintPathStorey);
                    }
                }

                var storey = deductWall.ElementAt(n);
                var wallCutPairs = storey.Value;

                for (int i = 0; i < wallCutPairs.Count(); i++)
                {
                    var wallCutPair = wallCutPairs.ElementAt(i);

                    var newWall = new List<Polygon>();
                    var onlyDelete = false;
                    if (debugMode == false)
                    {
                        newWall.AddRange(DeductService.CutBimWallGeom(wallCutPair.Key, wallCutPair.Value, out onlyDelete));
                    }
                    else
                    {
                        newWall.AddRange(DeductService.CutBimWallGeom(wallCutPair.Key, wallCutPair.Value, out onlyDelete, i, debugPrintPathStorey));
                    }


                    //检查门窗
                    var doorNewWallDict = new Dictionary<Polygon, List<DeductGFCModel>>();
                    foreach (var door in wallCutPair.Key.ChildItems)
                    {
                        if (ModelList.TryGetValue(door, out var doorModel))
                        {
                            var checkContainNewWall = CheckDoorToNewWall(doorModel.Outline, newWall);

                            if (checkContainNewWall != null)
                            {
                                //包含门窗的新墙
                                if (doorNewWallDict.ContainsKey(checkContainNewWall) == false)
                                {
                                    doorNewWallDict.Add(checkContainNewWall, new List<DeductGFCModel>());
                                }
                                doorNewWallDict[checkContainNewWall].Add(doorModel);
                            }
                            else
                            {
                                //删除门窗和楼层关系写在后面updateRelationship

                            }
                        }
                        else
                        {
                            //删除门窗和楼层关系写在后面updateRelationship
                        }
                    }


                    var newWallModel = new List<DeductGFCModel>();
                    if (onlyDelete == false)
                    {
                        newWallModel.AddRange(DeductService.ToWallModel(wallCutPair.Key, newWall));
                        foreach (var nwModel in newWallModel)
                        {
                            var doorSelect = doorNewWallDict.Where(x => x.Key == nwModel.Outline).FirstOrDefault();
                            if (doorSelect.Key != null)
                            {
                                doorSelect.Value.ForEach(x => nwModel.ChildItems.Add(x.UID));
                            }
                        }
                    }
                    wallCutDict.Add(wallCutPair.Key.UID, new Tuple<bool, List<DeductGFCModel>>(onlyDelete, newWallModel));
                }
            }

            return wallCutDict;
        }

        /// <summary>
        /// 返回新的包括门的墙，如果没有则为null
        /// </summary>
        /// <param name="doorModel"></param>
        /// <param name="newWall"></param>
        /// <returns></returns>
        private static Polygon CheckDoorToNewWall(Polygon doorModel, List<Polygon> newWall)
        {
            Polygon containW = null;

            foreach (var w in newWall)
            {
                var buffW = w.Buffer(1);
                if (buffW.Contains(doorModel))
                {
                    containW = w;
                    break;
                }
            }

            return containW;
        }

        public void UpdateRelationship(List<DeductGFCModel> archiStorey, Dictionary<string, Tuple<bool, List<DeductGFCModel>>> wallCutResult)
        {
            foreach (var wallCut in wallCutResult)
            {
                var newWallList = wallCut.Value.Item2;
                if (newWallList.Count > 0)
                {
                    var storeyHasOriWall = archiStorey.Where(x => x.ChildItems.Contains(wallCut.Key)).FirstOrDefault();
                    if (storeyHasOriWall != null)
                    {
                        var wallOri = ModelList[wallCut.Key];
                        var doorOriList = ModelList.Where(x => wallOri.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();
                        var doorList = new List<DeductGFCModel>();
                        foreach (var nw in newWallList)
                        {
                            storeyHasOriWall.ChildItems.Add(nw.UID);
                            ModelList.Add(nw.UID, nw);
                            doorList.AddRange(nw.ChildItems.Select(x => ModelList[x]));
                        }
                        var removeDoor = doorOriList.Except(doorList).ToList();

                        storeyHasOriWall.ChildItems.Remove(wallCut.Key);
                        ModelList.Remove(wallCut.Key);
                        removeDoor.ForEach(x =>
                        {
                            storeyHasOriWall.ChildItems.Remove(x.UID);
                            ModelList.Remove(x.UID);
                        });
                    }
                }
                else if (wallCut.Value.Item1 == true)
                {
                    //删掉墙
                    var storeyHasOriWall = archiStorey.Where(x => x.ChildItems.Contains(wallCut.Key)).FirstOrDefault();
                    if (storeyHasOriWall != null)
                    {
                        var wallOri = ModelList[wallCut.Key];
                        var doorOriList = ModelList.Where(x => wallOri.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();

                        storeyHasOriWall.ChildItems.Remove(wallCut.Key);
                        ModelList.Remove(wallCut.Key);
                        doorOriList.ForEach(x =>
                        {
                            storeyHasOriWall.ChildItems.Remove(x.UID);
                            ModelList.Remove(x.UID);
                        });

                    }
                }
            }
        }
    }
}
