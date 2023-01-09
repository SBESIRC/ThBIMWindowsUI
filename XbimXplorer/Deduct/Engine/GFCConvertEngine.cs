using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private Dictionary<string, DeductGFCModel> ModelList;
        private int globalId;

        Dictionary<int, List<int>> buildingStoreyGFCDict;//building gfc lineNo，floor gfc lineNo;
        Dictionary<int, List<GFCElementModel>> floorEntityDict;//floor gfc lineNo, Construct Model;
        List<RoomFurnishModel> roomFurnishList;

        public GFCConvertEngine(Dictionary<string, DeductGFCModel> modelList, string docPath)
        {
            ModelList = modelList;
            this.docPath = docPath;

            globalId = 0;
            buildingStoreyGFCDict = new Dictionary<int, List<int>>();
            floorEntityDict = new Dictionary<int, List<GFCElementModel>>();

            roomFurnishList = new List<RoomFurnishModel>();
        }

        public void ToGFCEngine(THBimProject project)
        {
            gfcDoc = ThGFC2Document.Create(docPath);
            try
            {
                GetRoomFurnishConfig();
                PrjToGFC(project);
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
            var prjName = archiProj.ProjectIdentity;
            THModelToGFC2.ToGfcProject(gfcDoc, globalId, prjName);
            globalId++;

            var building = ModelList.Where(x => x.Value.ItemType == DeductType.Building).ToList();

            for (int i = 0; i < building.Count; i++)
            {
                var buildingPair = building[i];
                var name = "building" + globalId;
                var buildingId = THModelToGFC2.ToGfcBuilding(gfcDoc, globalId, name);
                globalId++;

                buildingStoreyGFCDict.Add(buildingId, new List<int>());

                var storeys = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey && buildingPair.Value.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();
                storeys = storeys.OrderBy(x => x.GlobalZ).ToList();

                for (int j = 0; j < storeys.Count; j++)
                {
                    //if (j > 1)
                    //{
                    //    continue;
                    //}

                    var storey = storeys[j];
                    var nextStorey = j + 1 < storeys.Count ? storeys[j + 1] : storeys[j];
                    var storeyName = storey.IFC.Name;
                    var storeyId = storey.ToGfcStorey(gfcDoc, globalId, storeyName, j + 1);
                    globalId++;
                    var storeyRoomFurnishList = roomFurnishList.Where(x => x.StoreyName == storeyName).ToDictionary(x => x.RoomName, x => x);

                    buildingStoreyGFCDict[buildingId].Add(storeyId);
                    floorEntityDict.Add(storeyId, new List<GFCElementModel>());

                    var storeyItemList = ModelList.Where(x => storey.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();

                    BuildArchiWallWithDoorWin(storeyId, storeyItemList);

                    BuildSlab(storeyId, storey, nextStorey, storeyName);

                    if (WithFitment == true)
                    {
                        BuildFurnish(storeyId, storeyItemList, storeyRoomFurnishList);
                    }
                }
            }

            CreateRelationship();

        }

        private void BuildArchiWallWithDoorWin(int storeyId, List<DeductGFCModel> storeyItemList)
        {
            //Step 1：创建本层的墙门窗构建
            var archiWallList = storeyItemList.Where(x => x.ItemType == DeductType.ArchiWall).ToList();
            BuildWallModel(storeyId, archiWallList);

            var windowList = ModelList.Where(x => archiWallList.SelectMany(w => w.ChildItems).Contains(x.Key)
                                                && x.Value.ItemType == DeductType.Window).Select(x => x.Value).ToList();
            BuildWindowModel(storeyId, windowList);

            var doorList = ModelList.Where(x => archiWallList.SelectMany(w => w.ChildItems).Contains(x.Key)
                                               && x.Value.ItemType == DeductType.Door).Select(x => x.Value).ToList();

            BuildDoorModel(storeyId, doorList);

            var constructList = floorEntityDict[storeyId].OfType<GFCElementModel>().ToList();//本层构建

            //Step 2：创建本层的墙门窗图元并建立联系
            for (int i = 0; i < archiWallList.Count; i++)
            {
                var archiWall = archiWallList[i];
                var wallModel = new GFCWallModel(gfcDoc, globalId, "", archiWall);
                globalId++;

                if (wallModel != null)
                {
                    wallModel.AddGFCItemToConstruct(constructList);// 建立 墙构建-墙图元 关系

                    var windows = windowList.Where(x => archiWall.ChildItems.Contains(x.UID)).ToList();
                    foreach (var win in windows)
                    {
                        var winModel = new GFCWindowModel(gfcDoc, globalId, "", win, archiWall.GlobalZ);
                        globalId++;
                        winModel.AddGFCItemToConstruct(constructList);

                        wallModel.RelationElements.Add(winModel);// 建立 墙图元-窗图元 关系
                    }

                    var doors = doorList.Where(x => archiWall.ChildItems.Contains(x.UID)).ToList();
                    foreach (var door in doors)
                    {
                        var doorModel = new GFCDoorModel(gfcDoc, globalId, "", door, archiWall.GlobalZ);
                        globalId++;
                        doorModel.AddGFCItemToConstruct(constructList);

                        wallModel.RelationElements.Add(doorModel);
                    }
                }
            }
        }

        private void BuildWallModel(int storeyId, List<DeductGFCModel> archiWallList)
        {
            var groupWall = archiWallList.GroupBy(x => (int)Math.Round(x.Width));
            foreach (var widthG in groupWall)
            {
                int width = widthG.Key;
                var name = String.Format("内墙{0}", width);
                var wallModel = new GFCWallModel(gfcDoc, globalId, name, width);
                globalId++;
                floorEntityDict[storeyId].Add(wallModel);
            }
        }

        private void BuildWindowModel(int storeyId, List<DeductGFCModel> windowList)
        {
            var groupWall = windowList.GroupBy(x => new
            {
                windowLength = (int)Math.Round(x.CenterLine.Length),
                windowHeight = (int)Math.Round(x.ZValue)
            });
            foreach (var pairKey in groupWall)
            {
                var windowLength = pairKey.Key.windowLength;
                var windowHeight = pairKey.Key.windowHeight;
                var windowModel = new GFCWindowModel(gfcDoc, globalId, "", windowLength, windowHeight);
                globalId++;
                floorEntityDict[storeyId].Add(windowModel);
            }
        }

        private void BuildDoorModel(int storeyId, List<DeductGFCModel> doorList)
        {
            var group = doorList.GroupBy(x => new
            {
                doorLength = (int)Math.Round(x.CenterLine.Length),
                doorHeight = (int)Math.Round(x.ZValue)
            });
            foreach (var pairKey in group)
            {
                var doorLength = pairKey.Key.doorLength;
                var doorHeight = pairKey.Key.doorHeight;
                var doorModel = new GFCDoorModel(gfcDoc, globalId, "", doorLength, doorHeight);
                globalId++;
                floorEntityDict[storeyId].Add(doorModel);
            }
        }

        private void BuildSlab(int storeyId, DeductGFCModel aStorey, DeductGFCModel nextAStorey, string storeyName)
        {
            var slabs = ModelList.Select(x => x.Value).Where(x => x.ItemType == DeductType.Slab).Where(x =>
            {
                var elevation = x.GlobalZ;
                var storeyElevation = aStorey.GlobalZ + aStorey.ZValue;
                if (elevation >= storeyElevation - aStorey.ZValue / 2 && elevation <= storeyElevation + nextAStorey.ZValue / 2)
                    return true;
                else
                    return false;

            }).ToList();

            BuildSlabModel(storeyId, slabs, storeyName);
            var slabConstructs = floorEntityDict[storeyId].OfType<GFCElementModel>().ToList();

            foreach (var slab in slabs)
            {
                var slabModel = new GFCSlabModel(gfcDoc, globalId, storeyName, slab);
                globalId++;
                slabModel.AddGFCItemToConstruct(slabConstructs);
            }
        }

        private void BuildSlabModel(int storeyId, List<DeductGFCModel> slabList, string storeyName)
        {
            var group = slabList.GroupBy(x => (int)Math.Round(x.ZValue)).ToList();
            foreach (var thicknessG in group)
            {
                int thickness = thicknessG.Key;
                var slabModel = new GFCSlabModel(gfcDoc, globalId, storeyName, thickness);
                globalId++;
                floorEntityDict[storeyId].Add(slabModel);
            }
        }

        /// <summary>
        /// 装修
        /// </summary>
        private void BuildFurnish(int storeyId, List<DeductGFCModel> storeyItemList, Dictionary<string, RoomFurnishModel> storeyRoomFurnishList)
        {
            var slabPrimitives = floorEntityDict[storeyId].OfType<GFCSlabModel>().SelectMany(o => o.Primitives);
            var slabs = slabPrimitives.Select(p => p.Model).ToList();
            var wallPrimitives = floorEntityDict[storeyId].OfType<GFCWallModel>().SelectMany(o => o.Primitives);
            var walls = wallPrimitives.Select(p => p.Model).ToList();
            var rooms = storeyItemList.Where(x => x.ItemType == DeductType.Room).ToList();

            BuildFurnishModel(rooms, storeyRoomFurnishList, storeyId);

            var constructList = floorEntityDict[storeyId].OfType<GFCElementModel>().ToList();

            //墙二维场景
            var wall2DSence = new ThNTSSpatialIndex(walls.Select(o => o.Outline).OfType<NetTopologySuite.Geometries.Geometry>().ToList());

            foreach (var room in rooms)
            {
                var roomFurnish = GetRoomFurnish(room.IFC.Description, storeyRoomFurnishList);

                //天棚图元
                if (roomFurnish.CeilingName != "")
                {
                    var slabContainers = CheckRoomToSlab(room, slabs);
                    if (slabContainers.Count() > 0)
                    {

                        var ceilingModel = new GFCCeilingModel(gfcDoc, globalId, roomFurnish.CeilingName, room, roomFurnish.CeilingThickness);
                        globalId++;
                        ceilingModel.AddGFCItemToConstruct(constructList);

                        slabContainers.ForEach(slabContainer =>
                        {
                            var slabModel = slabPrimitives.FirstOrDefault(o => o.Model == slabContainer);
                            if (slabModel != null)
                            {
                                slabModel.RelationElements.Add(ceilingModel);
                            }
                        });
                    }
                }

                //底板图元
                if (roomFurnish.FloorFinishName != "")
                {
                    var faceFloorFinishModel = new GFCFloorFinishModel(gfcDoc, globalId, roomFurnish.FloorFinishName, room, roomFurnish.FloorFinishThickness);
                    globalId++;
                    faceFloorFinishModel.AddGFCItemToConstruct(constructList);
                }

                if (roomFurnish.WallFinishName != "")
                {
                    //墙面图元
                    var roomBufferPL = room.Outline.NTSBuffer(1).Shell;
                    var adjacentWalls = wall2DSence.SelectFence(roomBufferPL);
                    foreach (var item in adjacentWalls)
                    {
                        var wall = walls.First(o => o.Outline == item);
                        var results = ThCoreNTSGeometryClipper.Clip(wall.Outline, roomBufferPL, false);
                        var intersectLines = results.OfType<LineString>().SelectMany(o => o.ToLines()).Where(o => o.Length > 100);
                        var WallModel = wallPrimitives.FirstOrDefault(o => o.Model.Outline == item);
                        foreach (var intersectLine in intersectLines)
                        {
                            var vector1 = intersectLine.EndPoint.ToXbimPoint() - intersectLine.StartPoint.ToXbimPoint();
                            var vector2 = WallModel.Model.CenterLine.P1.ToXbimPoint() - WallModel.Model.CenterLine.P0.ToXbimPoint();
                            if (!vector1.IsParallel(vector2, THBimDomainCommon.AngleTolerance))
                            {
                                continue;
                            }

                            var wallFinishModel = new GFCWallFinishModel(gfcDoc, globalId, roomFurnish.WallFinishName, WallModel.Model, roomFurnish.WallFinishThickness, intersectLine);
                            globalId++;
                            wallFinishModel.AddGFCItemToConstruct(constructList);
                            WallModel.RelationElements.Add(wallFinishModel);
                        }
                    }
                }
            }
        }

        private void BuildFurnishModel(List<DeductGFCModel> rooms, Dictionary<string, RoomFurnishModel> storeyRoomFurnishList, int storeyId)
        {
            var roomNameList = rooms.Select(x => x.IFC.Description).ToList();
            var roomFurnishList = roomNameList.Select(x => GetRoomFurnish(x, storeyRoomFurnishList)).ToList();


            var ceilingFurnish = roomFurnishList.Where(x => x.CeilingName != "").GroupBy(x => x.CeilingName).ToList();
            foreach (var ceiling in ceilingFurnish)
            {
                BuildCeilingModel(storeyId, ceiling.First().CeilingName, ceiling.First().CeilingThickness);
            }

            var floorFinish = roomFurnishList.Where(x => x.FloorFinishName != "").GroupBy(x => x.FloorFinishName).ToList();
            foreach (var floor in floorFinish)
            {
                BuildFloorFinishModel(storeyId, floor.First().FloorFinishName, floor.First().FloorFinishThickness);
            }

            var wallFinish = roomFurnishList.Where(x => x.WallFinishName != "").GroupBy(x => x.WallFinishName).ToList();
            foreach (var wallF in wallFinish)
            {
                BuildWallFinishModel(storeyId, wallF.First().WallFinishName, wallF.First().WallFinishThickness);
            }
        }

        private static RoomFurnishModel GetRoomFurnish(string roomName, Dictionary<string, RoomFurnishModel> storeyRoomFurnishList)
        {
            var roomNameList = roomName.Split(',').ToList();
            roomNameList = roomNameList.Select(x => x.Trim()).ToList();

            RoomFurnishModel roomFurnish = null;

            foreach (var roomN in roomNameList)
            {
                storeyRoomFurnishList.TryGetValue(roomN, out roomFurnish);
                if (roomFurnish != null)
                {
                    break;
                }
            }

            if (roomFurnish == null)
            {
                storeyRoomFurnishList.TryGetValue("默认房间", out roomFurnish);
            }

            return roomFurnish;
        }

        private static List<DeductGFCModel> CheckRoomToSlab(DeductGFCModel room, List<DeductGFCModel> slabs)
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

        private void BuildCeilingModel(int storeyId, string name, int thickness)
        {
            var model = new GFCCeilingModel(gfcDoc, globalId, name, thickness);
            globalId++;
            floorEntityDict[storeyId].Add(model);
        }

        private void BuildFloorFinishModel(int storeyId, string name, int thickness)
        {
            var model = new GFCFloorFinishModel(gfcDoc, globalId, name, thickness);
            globalId++;
            floorEntityDict[storeyId].Add(model);
        }

        private void BuildWallFinishModel(int storeyId, string wallFaceFinishName, int thickness)
        {
            var model = new GFCWallFinishModel(gfcDoc, globalId, wallFaceFinishName, thickness);
            globalId++;
            floorEntityDict[storeyId].Add(model);
        }

        private void CreateRelationship()
        {
            foreach (var p in buildingStoreyGFCDict)
            {
                gfcDoc.AddRelAggregate(p.Key, p.Value);
            }
            foreach (var floorEntityKeyValuePair in floorEntityDict)
            {
                //拿到该楼层所有图元
                var wallConstructs = floorEntityKeyValuePair.Value.OfType<GFCWallModel>();
                var doorConstructs = floorEntityKeyValuePair.Value.OfType<GFCDoorModel>();
                var windowConstructs = floorEntityKeyValuePair.Value.OfType<GFCWindowModel>();
                var slabConstructs = floorEntityKeyValuePair.Value.OfType<GFCSlabModel>();
                var ceilingConstructs = floorEntityKeyValuePair.Value.OfType<GFCCeilingModel>();
                var faceFloorFinishConstructs = floorEntityKeyValuePair.Value.OfType<GFCFloorFinishModel>();
                var wallFaceFinishConstructs = floorEntityKeyValuePair.Value.OfType<GFCWallFinishModel>();

                gfcDoc.AddRelAggregate(floorEntityKeyValuePair.Key, floorEntityKeyValuePair.Value.Union(floorEntityKeyValuePair.Value.SelectMany(o => o.Primitives)).Select(o => o.ID).ToList());//建立【楼层/所有构建+图元】关系

                foreach (var wallConstruct in wallConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(wallConstruct.ID, wallConstruct.Primitives.Select(o => o.ID).ToList());//建立【墙构建/墙图元】关系
                    wallConstruct.Primitives.ForEach(wall =>
                    {
                        wall.RelationElements.ForEach(o =>
                        {
                            gfcDoc.AddRelNest(wall.ID, o.ID);//建立【墙图元/(墙面/门/窗)图元】关系
                        });
                    });
                }
                foreach (var doorConstruct in doorConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(doorConstruct.ID, doorConstruct.Primitives.Select(o => o.ID).ToList());//建立【门构建/门图元】关系
                }
                foreach (var windowConstruct in windowConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(windowConstruct.ID, windowConstruct.Primitives.Select(o => o.ID).ToList());//建立【窗构建/窗图元】关系
                }
                foreach (var slabConstruct in slabConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(slabConstruct.ID, slabConstruct.Primitives.Select(o => o.ID).ToList());//建立【板构建/板图元】关系
                    slabConstruct.Primitives.ForEach(slab =>
                    {
                        slab.RelationElements.ForEach(o =>
                        {
                            gfcDoc.AddRelNest(slab.ID, o.ID);//建立【板图元/天棚图元】关系
                        });
                    });
                }
                foreach (var ceilingConstruct in ceilingConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(ceilingConstruct.ID, ceilingConstruct.Primitives.Select(o => o.ID).ToList());//建立【天棚构建/天棚图元】关系
                }
                foreach (var faceFloorFinishConstruct in faceFloorFinishConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(faceFloorFinishConstruct.ID, faceFloorFinishConstruct.Primitives.Select(o => o.ID).ToList());//建立【底板构建/底板图元】关系
                }
                foreach (var wallFaceFinishConstruct in wallFaceFinishConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(wallFaceFinishConstruct.ID, wallFaceFinishConstruct.Primitives.Select(o => o.ID).ToList());//建立【墙面构建/墙面图元】关系
                }
            }
        }

        /// <summary>
        /// 这里读房间装修配置
        /// 注意每层需要给个defaul配置
        /// </summary>
        private void GetRoomFurnishConfig()
        {
            AddDefultRoomConfig();

            var storeysList = new List<DeductGFCModel>();
            var building = ModelList.Where(x => x.Value.ItemType == DeductType.Building).ToList();
            for (int i = 0; i < building.Count; i++)
            {
                var buildingPair = building[i];
                var storeys = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey && buildingPair.Value.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();
                storeys = storeys.OrderBy(x => x.GlobalZ).ToList();
                storeysList.AddRange(storeys);
            }

            var roomFurnish = new RoomFurnishModel();
            roomFurnish.StoreyName = storeysList[0].IFC.Name;
            roomFurnish.RoomName = "卧室";
            roomFurnish.CeilingName = "天棚1";
            roomFurnish.CeilingThickness = 6;
            roomFurnish.FloorFinishName = "楼地面1";
            roomFurnish.FloorFinishThickness = 6;
            roomFurnish.WallFinishName = "墙面1";
            roomFurnish.WallFinishThickness = 6;

            var roomFurnish2 = new RoomFurnishModel();
            roomFurnish2.StoreyName = storeysList[0].IFC.Name;
            roomFurnish2.RoomName = "阳台";
            roomFurnish2.CeilingName = "天棚1";
            roomFurnish2.CeilingThickness = 6;
            roomFurnish2.FloorFinishName = "楼地面1";
            roomFurnish2.FloorFinishThickness = 6;

            var roomFurnish3 = new RoomFurnishModel();
            roomFurnish3.StoreyName = storeysList[1].IFC.Name;
            roomFurnish3.RoomName = "起居室";
            roomFurnish3.CeilingName = "天棚4";
            roomFurnish3.CeilingThickness = 8;
            roomFurnish3.FloorFinishName = "楼地面4";
            roomFurnish3.FloorFinishThickness = 8;
            roomFurnish3.WallFinishName = "墙面4";
            roomFurnish3.WallFinishThickness = 8;

            roomFurnishList.Add(roomFurnish);
            roomFurnishList.Add(roomFurnish2);
            roomFurnishList.Add(roomFurnish3);
        }


        private void AddDefultRoomConfig()
        {
            var building = ModelList.Where(x => x.Value.ItemType == DeductType.Building).ToList();

            for (int i = 0; i < building.Count; i++)
            {
                var buildingPair = building[i];
                var storeys = ModelList.Where(x => x.Value.ItemType == DeductType.ArchiStorey && buildingPair.Value.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();
                storeys = storeys.OrderBy(x => x.GlobalZ).ToList();

                for (int j = 0; j < storeys.Count; j++)
                {
                    var storey = storeys[j];

                    var roomFurnish = new RoomFurnishModel();
                    roomFurnish.StoreyName = storey.IFC.Name;
                    roomFurnish.RoomName = "默认房间";
                    roomFurnish.CeilingName = "天棚默认";
                    roomFurnish.CeilingThickness = j + 1;
                    roomFurnish.FloorFinishName = "楼地面默认";
                    roomFurnish.FloorFinishThickness = j + 1;
                    roomFurnish.WallFinishName = "墙面默认";
                    roomFurnish.WallFinishThickness = j + 1;

                    roomFurnishList.Add(roomFurnish);
                }
            }
        }
    }
}
