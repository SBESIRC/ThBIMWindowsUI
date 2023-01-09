using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using XbimXplorer.Deduct.Model;

namespace XbimXplorer.Deduct
{
    internal class DeductCommonService
    {
        public static Dictionary<Polygon, List<DeductGFCModel>> LinkDoorToNewWall(List<DeductGFCModel> DoorList, List<Polygon> newWall)
        {
            //检查门窗
            var doorNewWallDict = new Dictionary<Polygon, List<DeductGFCModel>>();
            foreach (var doorModel in DoorList)
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
            return doorNewWallDict;
        }

        public static List<DeductGFCModel> CreateNewWall(DeductGFCModel oriWall, List<Polygon> newWall, Dictionary<Polygon, List<DeductGFCModel>> doorLink)
        {
            var newWallModel = new List<DeductGFCModel>();
            newWallModel.AddRange(DeductService.ToWallModel(oriWall, newWall));
            foreach (var nwModel in newWallModel)
            {
                var doorSelect = doorLink.Where(x => x.Key == nwModel.Outline).FirstOrDefault();
                if (doorSelect.Key != null)
                {
                    doorSelect.Value.ForEach(x => nwModel.ChildItems.Add(x.UID));
                }
            }

            return newWallModel;
        }

        public static void UpdateRelationship(Dictionary<string, DeductGFCModel> ModelList, List<DeductGFCModel> archiStorey, Dictionary<string, Tuple<bool, List<DeductGFCModel>>> wallCutResult)
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
        public static void DebugScript(List<Polygon> pls, string name, int color, int printC, ref string script)
        {
            var localS = "";
            for (int i = 0; i < pls.Count; i++)
            {
                var pName = name + i.ToString() + printC;
                localS += string.Format(@"var {0} = new Polyline();", pName) + System.Environment.NewLine;
                foreach (var p in pls[i].Coordinates)
                {
                    var ptScript = string.Format("{0}.AddVertexAt({0}.NumberOfVertices, new Point2d({1}, {2}), 0, 0, 0);",
                                            pName, p.X, p.Y);

                    localS += ptScript + System.Environment.NewLine;
                }
            }
            for (int i = 0; i < pls.Count; i++)
            {
                var pName = name + i.ToString() + printC;
                var dS = string.Format(@"DrawUtils.ShowGeometry({0}, ""{1}"", {2});", pName, name, color);
                localS += dS + System.Environment.NewLine;
            }

            script += localS + System.Environment.NewLine;
        }

    }
}
