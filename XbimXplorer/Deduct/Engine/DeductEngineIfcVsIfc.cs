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

            DeductCommonService.UpdateRelationship(ModelList, archiStorey, wallCutResult);

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

                    var doorList = ModelList.Where(x => wallCutPair.Key.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();
                    var doorNewWallDict = DeductCommonService.LinkDoorToNewWall(doorList, newWall);

                    var newWallModel = new List<DeductGFCModel>();
                    if (onlyDelete == false)
                    {
                        newWallModel.AddRange(DeductCommonService.CreateNewWall(wallCutPair.Key, newWall, doorNewWallDict));
                    }
                    wallCutDict.Add(wallCutPair.Key.UID, new Tuple<bool, List<DeductGFCModel>>(onlyDelete, newWallModel));
                }
            }

            return wallCutDict;
        }
    }
}
