using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;

using THBimEngine.Domain;
using THBimEngine.IO.GFC2;
using THBimEngine.IO.NTS;
using ThBIMServer.NTS;
using XbimXplorer.Deduct.Model;

namespace XbimXplorer.Deduct
{
    public class GFCConvertEngine
    {
        public bool WithFitment; //临时的参数 false： 不带装修， true 带装修
        string docPath;
        private ThGFC2Document gfcDoc;
        Dictionary<string, DeductGFCModel> ModelList;

        Dictionary<int, List<int>> buildingStoreyGFCDict;//building gfc lineNo，floor gfc lineNo;
        Dictionary<int, List<int>> floorEntityDict;//floor gfc lineNo, entity gfc lineNo;
        Dictionary<int, Dictionary<int, Tuple<int, List<int>>>> archiWallModelDict;//key：wall width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;
        Dictionary<int, Dictionary<Tuple<double, double>, Tuple<int, List<int>>>> doorModelDict;//key：door width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;
        Dictionary<int, Dictionary<Tuple<double, double>, Tuple<int, List<int>>>> windowModelDict;//key：window width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;
        Dictionary<int, Dictionary<int, Tuple<int, List<int>>>> slabModelDict;
        Dictionary<int, Dictionary<int, Tuple<int, List<int>>>> wallFaceFinishModelDict;
        Dictionary<int, Dictionary<int, Tuple<int, List<int>>>> ceilingModelDict;
        Dictionary<int, Dictionary<int, Tuple<int, List<int>>>> faceFloorFinishModelDict;
        Dictionary<DeductGFCModel, int> archiWallDict;
        Dictionary<int, Dictionary<string, Tuple<int, List<int>>>> roomModelDict;

        int globalId;

        public GFCConvertEngine(Dictionary<string, DeductGFCModel> ModelList, string docPath)
        {
            WithFitment = false;
            this.ModelList = ModelList;
            this.docPath = docPath;

            buildingStoreyGFCDict = new Dictionary<int, List<int>>();//building gfc lineNo，floor gfc lineNo;
            floorEntityDict = new Dictionary<int, List<int>>();//floor gfc lineNo, entity gfc lineNo;
            archiWallModelDict = new Dictionary<int, Dictionary<int, Tuple<int, List<int>>>>();//key：wall width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;
            doorModelDict = new Dictionary<int, Dictionary<Tuple<double, double>, Tuple<int, List<int>>>>();//key：door width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;
            windowModelDict = new Dictionary<int, Dictionary<Tuple<double, double>, Tuple<int, List<int>>>>();//key：window width ，item1: wallmodel lineNo, item2: wall entity gfc lineNo;
            slabModelDict = new Dictionary<int, Dictionary<int, Tuple<int, List<int>>>>();//key:storey id. key:slab width,item1:slabmodel lineNo,item2:slab entity lineNo
            wallFaceFinishModelDict = new Dictionary<int, Dictionary<int, Tuple<int, List<int>>>>();//key:storey id. key:slab width,item1:slabmodel lineNo,item2:slab entity lineNo
            ceilingModelDict = new Dictionary<int, Dictionary<int, Tuple<int, List<int>>>>();
            faceFloorFinishModelDict = new Dictionary<int, Dictionary<int, Tuple<int, List<int>>>>();
            archiWallDict = new Dictionary<DeductGFCModel, int>();
            roomModelDict = new Dictionary<int, Dictionary<string, Tuple<int, List<int>>>>();

            globalId = 0;

        }

        public void ToGFCEngine(THBimProject archiProj)
        {
            gfcDoc = ThGFC2Document.Create(docPath);
            try
            {
                PrjToGFC(archiProj);
            }
            catch (Exception ex)
            {
                var msg = ex.StackTrace.ToString();
            }
            finally
            {
                gfcDoc.Close();
            }
        }

        private void PrjToGFC(THBimProject archiProj)
        {
            archiProj.ToGfc(gfcDoc, ref globalId);
            var building = ModelList.Where(x => x.Value.ItemType == DeductType.Building).ToList();


            for (int i = 0; i < building.Count; i++)
            {
                var buildingPair = building[i];
                var buildingId = buildingPair.Value.ToGfcBuilding(gfcDoc, ref globalId);
                buildingStoreyGFCDict.Add(buildingId, new List<int>());

                var storeys = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey && buildingPair.Value.ChildItems.Contains(x.Key)).ToList();
                storeys = storeys.OrderBy(x => x.Value.GlobalZ).ToList();
                for (int j = 0; j < storeys.Count; j++)
                {
                    //if (j > 0)
                    //{
                    //    continue;
                    //}

                    var storeyPair = storeys[j];
                    var nextStoreyPair = j + 1 < storeys.Count ? storeys[j + 1] : storeys[j];
                    var storeyName = storeyPair.Value.IFC.Name;
                    var storeyId = storeyPair.Value.ToGfcStorey(gfcDoc, ref globalId, storeyName, j + 1);
                    buildingStoreyGFCDict[buildingId].Add(storeyId);
                    floorEntityDict.Add(storeyId, new List<int>());

                    var storeyItemList = ModelList.Where(x => storeyPair.Value.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();

                    BuildArchiWallWithDoorWin(storeyId, storeyItemList);

                    if (WithFitment == true)
                    {
                        //装修部分(这几个地方后期要考虑一下融合，因为互相之间共用的东西太多了)
                        var slabIDDict = BuildSlab(storeyId, storeyItemList, storeyPair.Value, nextStoreyPair.Value, storeyName);
                        BuildCeiling(storeyId, storeyItemList, slabIDDict);
                        BuildFaceFloorFinish(storeyId, storeyItemList);
                        BuildFurnish(storeyId, storeyItemList, storeyPair.Value, storeyName);
                    }
                }
            }

            CreateRelationship();

        }

        private void BuildArchiWallWithDoorWin(int storeyId, List<DeductGFCModel> storeyItemList)
        {
            archiWallModelDict.Add(storeyId, new Dictionary<int, Tuple<int, List<int>>>());
            windowModelDict.Add(storeyId, new Dictionary<Tuple<double, double>, Tuple<int, List<int>>>());
            doorModelDict.Add(storeyId, new Dictionary<Tuple<double, double>, Tuple<int, List<int>>>());

            var archiWallList = storeyItemList.Where(x => x.ItemType == DeductType.ArchiWall).ToList();
            BuildWallModel(storeyId, archiWallList);

            var windowList = ModelList.Where(x => archiWallList.SelectMany(w => w.ChildItems).Contains(x.Key)
                                                && x.Value.ItemType == DeductType.Window).Select(x => x.Value).ToList();
            BuildWindowModel(storeyId, windowList);

            var doorList = ModelList.Where(x => archiWallList.SelectMany(w => w.ChildItems).Contains(x.Key)
                                               && x.Value.ItemType == DeductType.Door).Select(x => x.Value).ToList();

            BuildDoorModel(storeyId, doorList);

            for (int i = 0; i < archiWallList.Count; i++)
            {
                var archiWall = archiWallList[i];
                var wallId = archiWall.ToGfcArchiWall(gfcDoc, ref globalId);
                if (wallId != -1)
                {
                    var width = (int)Math.Round(archiWall.Width);
                    floorEntityDict[storeyId].Add(wallId);
                    archiWallModelDict[storeyId][width].Item2.Add(wallId);
                    archiWallDict.Add(archiWall, wallId);
                    var windows = windowList.Where(x => archiWall.ChildItems.Contains(x.UID)).ToList();
                    foreach (var win in windows)
                    {
                        var winId = win.ToGfcWindow(gfcDoc, archiWall.GlobalZ, ref globalId);
                        floorEntityDict[storeyId].Add(winId);
                        var length = Math.Round(win.CenterLine.Length);
                        var height = Math.Round(win.ZValue);
                        windowModelDict[storeyId][new Tuple<double, double>(length, height)].Item2.Add(winId);
                        gfcDoc.AddRelNests(wallId, new List<int>() { winId });
                    }

                    var doors = doorList.Where(x => archiWall.ChildItems.Contains(x.UID)).ToList();
                    foreach (var door in doors)
                    {
                        var doorId = door.ToGfcDoor(gfcDoc, archiWall.GlobalZ, ref globalId);
                        floorEntityDict[storeyId].Add(doorId);
                        var length = Math.Round(door.CenterLine.Length);
                        var height = Math.Round(door.ZValue);
                        doorModelDict[storeyId][new Tuple<double, double>(length, height)].Item2.Add(doorId);
                        gfcDoc.AddRelNests(wallId, new List<int>() { doorId });
                    }
                }
            }
        }

        private void BuildWallModel(int storeyId, List<DeductGFCModel> archiWallList)
        {
            var groupWall = archiWallList.GroupBy(x => (int)Math.Round(x.Width));
            foreach (var widthG in groupWall)
            {
                var matirial = "";
                int width = widthG.Key;
                var leftWidth = (int)width / 2;

                var modelId = THModelToGFC2.ToGfcArchiWallModel(gfcDoc, matirial, ref globalId, leftWidth, width);
                archiWallModelDict[storeyId].Add(width, new Tuple<int, List<int>>(modelId, new List<int>()));
                floorEntityDict[storeyId].Add(modelId);
            }
        }

        private void BuildWindowModel(int storeyId, List<DeductGFCModel> windowList)
        {
            var groupWall = windowList.GroupBy(x => new
            {
                windowLength = Math.Round(x.CenterLine.Length),
                height = Math.Round(x.ZValue)
            });
            foreach (var pairKey in groupWall)
            {
                var windowLength = pairKey.Key.windowLength;
                var height = pairKey.Key.height;

                var modelId = THModelToGFC2.ToGfcWindowModel(gfcDoc, windowLength, height, ref globalId);
                windowModelDict[storeyId].Add(new Tuple<double, double>(windowLength, height), new Tuple<int, List<int>>(modelId, new List<int>()));
                floorEntityDict[storeyId].Add(modelId);
            }
        }

        private void BuildDoorModel(int storeyId, List<DeductGFCModel> doorList)
        {
            var group = doorList.GroupBy(x => new
            {
                doorLength = Math.Round(x.CenterLine.Length, 1),
                height = Math.Round(x.ZValue, 1)
            });
            foreach (var pairKey in group)
            {
                var doorLength = pairKey.Key.doorLength;
                var height = pairKey.Key.height;

                var modelId = THModelToGFC2.ToGfcDoorModel(gfcDoc, doorLength, height, ref globalId);
                doorModelDict[storeyId].Add(new Tuple<double, double>(doorLength, height), new Tuple<int, List<int>>(modelId, new List<int>()));
                floorEntityDict[storeyId].Add(modelId);
            }
        }

        /// <summary>
        /// 装修(墙面)
        /// </summary>
        private void BuildFurnish(int storeyId, List<DeductGFCModel> storeyItemList, DeductGFCModel aStorey, string storeyName)
        {
            var wallFaceFinishDict = new Dictionary<string, int>();//墙面
            wallFaceFinishModelDict.Add(storeyId, new Dictionary<int, Tuple<int, List<int>>>());
            var rooms = storeyItemList.Where(x => x.ItemType == DeductType.Room).ToList();
            var archiWallList = storeyItemList.Where(x => x.ItemType == DeductType.ArchiWall).ToList();
            var wall2DSence = new ThNTSSpatialIndex(archiWallList.Select(o => o.Outline)
                .OfType<NetTopologySuite.Geometries.Geometry>().ToList());

            var modelId = THModelToGFC2.ToGfcWallFaceFinishModel(gfcDoc, ref globalId, "墙面1", 0);
            wallFaceFinishModelDict[storeyId].Add(0, new Tuple<int, List<int>>(modelId, new List<int>()));
            floorEntityDict[storeyId].Add(modelId);

            foreach (var room in rooms)
            {
                var roomBufferPL = room.Outline.NTSBuffer(1).Shell;
                var adjacentWalls = wall2DSence.SelectFence(roomBufferPL);
                List<NetTopologySuite.Geometries.Geometry> geos = new List<NetTopologySuite.Geometries.Geometry>();
                geos.AddRange(adjacentWalls);
                foreach (var item in adjacentWalls)
                {
                    var wallModel = archiWallList.First(o => o.Outline == item);
                    var results = ThCoreNTSGeometryClipper.Clip(wallModel.Outline, roomBufferPL, false);
                    var intersectLines = results.OfType<LineString>().SelectMany(o => o.ToLines()).Where(o => o.Length > 100);
                    var archiWallModel = archiWallList.FirstOrDefault(o => o.Outline == item);
                    foreach (var intersectLine in intersectLines)
                    {
                        var vector1 = intersectLine.EndPoint.ToXbimPoint() - intersectLine.StartPoint.ToXbimPoint();
                        var vector2 = archiWallModel.CenterLine.P1.ToXbimPoint() - archiWallModel.CenterLine.P0.ToXbimPoint();
                        if (!vector1.IsParallel(vector2, THBimDomainCommon.AngleTolerance))
                        {
                            continue;
                        }
                        geos.Add(intersectLine);
                        var wallFaceFinishId = archiWallModel.ToGfcWallFaceFinish(gfcDoc, ref globalId, "", intersectLine);
                        floorEntityDict[storeyId].Add(wallFaceFinishId);
                        wallFaceFinishModelDict[storeyId][0].Item2.Add(wallFaceFinishId);
                        gfcDoc.AddRelNest(archiWallDict[archiWallModel], wallFaceFinishId);
                        //archiWallDict[archiWallModel] = (archiWallDict[archiWallModel].Item1,wallFaceFinishId).ToTuple();
                    }
                }
                //打印房间和周边墙的数据
                //var stream = File.Create(@"D:\My\GFC\test.wkt");
                //geos.Add(roomBufferPL);
                //using (StreamWriter writer = new StreamWriter(stream))
                //{
                //    var wkt = new NetTopologySuite.IO.WKTWriter();
                //    NetTopologySuite.Geometries.Geometry[] geometries = geos.ToArray();
                //    //NetTopologySuite.Geometries.Geometry[] geometries = new NetTopologySuite.Geometries.Geometry[21];
                //    var geos1 = ThIFCNTSService.Instance.GeometryFactory.CreateGeometryCollection(geometries);
                //    wkt.WriteFormatted(geos1, writer);

                //}
            }
        }

        private Dictionary<string, int> BuildSlab(int storeyId, List<DeductGFCModel> storeyItemList, DeductGFCModel aStorey, DeductGFCModel nextAStorey, string storeyName)
        {
            var slabDict = new Dictionary<string, int>();
            slabModelDict.Add(storeyId, new Dictionary<int, Tuple<int, List<int>>>());
            var slabs = ModelList.Select(x => x.Value).Where(x => x.ItemType == DeductType.Slab).Where(x =>
            {
                var elevation = x.GlobalZ;
                var storeyElevation = aStorey.GlobalZ + aStorey.ZValue;
                //var height = elevation - storeyElevation;
                if (elevation >= storeyElevation - aStorey.ZValue / 2 && elevation <= storeyElevation + nextAStorey.ZValue / 2)
                    return true;
                else
                    return false;

            }).ToList();

            BuildSlabModel(storeyId, slabs, storeyName);
            foreach (var slab in slabs)
            {
                var slabId = slab.ToGfcSlab(gfcDoc, ref globalId, storeyName);
                var thickness = (int)Math.Round(slab.ZValue);
                floorEntityDict[storeyId].Add(slabId);
                slabModelDict[storeyId][thickness].Item2.Add(slabId);
                slabDict.Add(slab.UID, slabId);
            }

            return slabDict;
        }

        private void BuildSlabModel(int storeyId, List<DeductGFCModel> slabList, string storeyName)
        {
            var group = slabList.GroupBy(x => (int)Math.Round(x.ZValue)).ToList();
            foreach (var thicknessG in group)
            {
                int thickness = thicknessG.Key;
                var modelId = THModelToGFC2.ToGfcSlabModel(gfcDoc, ref globalId, storeyName, thickness);
                slabModelDict[storeyId].Add(thickness, new Tuple<int, List<int>>(modelId, new List<int>()));
                floorEntityDict[storeyId].Add(modelId);
            }
        }

        private void BuildCeiling(int storeyId, List<DeductGFCModel> storeyItemList, Dictionary<string, int> slabDict)
        {
            ceilingModelDict.Add(storeyId, new Dictionary<int, Tuple<int, List<int>>>());

            //这里暂定所有的装修厚度都是一样的，之后可能根据做法分
            var ceilingThickness = 10;
            BuildCeilingModel(storeyId, ceilingThickness);

            var slabs = ModelList.Where(x => slabDict.Keys.Contains(x.Key)).Select(x => x.Value).ToList();

            var rooms = storeyItemList.Where(x => x.ItemType == DeductType.Room).ToList();
            foreach (var room in rooms)
            {
                var slabContainers = CheckRoomToSlab(room, slabs);
                if (slabContainers.Count() > 0)
                {
                    var ceilingId = room.ToGfcCeiling(gfcDoc, ref globalId, ceilingThickness);
                    floorEntityDict[storeyId].Add(ceilingId);
                    ceilingModelDict[storeyId][ceilingThickness].Item2.Add(ceilingId);

                    slabContainers.ForEach(slabContainer =>
                    {
                        if (!slabDict.ContainsKey(slabContainer.UID))
                        {
                            return;
                        }
                        gfcDoc.AddRelNests(slabDict[slabContainer.UID], new List<int>() { ceilingId });
                    });
                }
            }
        }


        private void BuildCeilingModel(int storeyId, int thickness)
        {
            var modelId = THModelToGFC2.ToGfcCeilingModel(gfcDoc, ref globalId, thickness);
            ceilingModelDict[storeyId].Add(thickness, new Tuple<int, List<int>>(modelId, new List<int>()));
            floorEntityDict[storeyId].Add(modelId);
        }

        private List<DeductGFCModel> CheckRoomToSlab(DeductGFCModel room, List<DeductGFCModel> slabs)
        {
            var results = new List<DeductGFCModel>();
            foreach (var slab in slabs)
            {
                var inter = room.Outline.Intersection(slab.Outline);
                if (inter.Area > 1.0)
                {
                    results.Add(slab);
                }
            }
            return results;
        }

        private void BuildFaceFloorFinish(int storeyId, List<DeductGFCModel> storeyItemList)
        {
            faceFloorFinishModelDict.Add(storeyId, new Dictionary<int, Tuple<int, List<int>>>());
            var rooms = storeyItemList.Where(x => x.ItemType == DeductType.Room).ToList();
            //这里暂定所有的装修厚度都是一样的，之后可能根据做法分
            var thickness = 0;
            BuildFaceFloorFinishModel(storeyId, thickness);
            foreach (var room in rooms)
            {
                var faceFloorFinishId = room.ToGfcFaceFloorFinish(gfcDoc, ref globalId, thickness);
                floorEntityDict[storeyId].Add(faceFloorFinishId);
                faceFloorFinishModelDict[storeyId][thickness].Item2.Add(faceFloorFinishId);
            }
        }

        private void BuildFaceFloorFinishModel(int storeyId, int thickness)
        {
            var modelId = THModelToGFC2.ToGfcFaceFloorFinishModel(gfcDoc, ref globalId, thickness);
            faceFloorFinishModelDict[storeyId].Add(thickness, new Tuple<int, List<int>>(modelId, new List<int>()));
            floorEntityDict[storeyId].Add(modelId);
        }

        private void BuildRoom(int storeyId, List<DeductGFCModel> storeyItemList)
        {
            roomModelDict.Add(storeyId, new Dictionary<string, Tuple<int, List<int>>>());
            var rooms = storeyItemList.Where(x => x.ItemType == DeductType.Room).ToList();
            //这里暂定所有的装修厚度都是一样的，之后可能根据做法分
            var thickness = 0;
            var name = "房间1";
            BuildRoomModel(storeyId);
            foreach (var room in rooms)
            {
                var roomFinishId = room.ToGfcRoom(gfcDoc, ref globalId, name, thickness);
                floorEntityDict[storeyId].Add(roomFinishId);
                roomModelDict[storeyId][name].Item2.Add(roomFinishId);
            }
        }

        private void BuildRoomModel(int storeyId)
        {
            var thickness = 10;
            var name = "房间1";
            var modelId = THModelToGFC2.ToGfcRoomModel(gfcDoc, ref globalId, name);

            roomModelDict[storeyId].Add(name, new Tuple<int, List<int>>(modelId, new List<int>()));
            floorEntityDict[storeyId].Add(modelId);
        }

        private void CreateRelationship()
        {
            foreach (var p in buildingStoreyGFCDict)
            {
                gfcDoc.AddRelAggregate(p.Key, p.Value);
            }

            foreach (var p in floorEntityDict)
            {
                gfcDoc.AddRelAggregate(p.Key, p.Value);
            }

            foreach (var p in archiWallModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in windowModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in doorModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in slabModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in ceilingModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in faceFloorFinishModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in wallFaceFinishModelDict)
            {
                AddRelDefinesByElementDict(p.Value);
            }
            foreach (var p in roomModelDict)
            {
                foreach (var model in p.Value)
                {
                    gfcDoc.AddRelDefinesByElement(model.Value.Item1, model.Value.Item2);
                }
            }
        }

        private void AddRelDefinesByElementDict(Dictionary<Tuple<double, double>, Tuple<int, List<int>>> dict)
        {
            foreach (var model in dict)
            {
                gfcDoc.AddRelDefinesByElement(model.Value.Item1, model.Value.Item2);
            }
        }

        private void AddRelDefinesByElementDict(Dictionary<int, Tuple<int, List<int>>> dict)
        {
            foreach (var model in dict)
            {
                gfcDoc.AddRelDefinesByElement(model.Value.Item1, model.Value.Item2);
            }
        }

    }
}
