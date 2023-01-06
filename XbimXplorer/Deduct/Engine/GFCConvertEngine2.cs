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
    public class GFCConvertEngine2
    {
        public bool WithFitment; //临时的参数 false： 不带装修， true 带装修
        private ThGFC2Document gfcDoc;
        private Dictionary<string, DeductGFCModel> ModelList;
        private int globalId;

        Dictionary<int, List<int>> buildingStoreyGFCDict;//building gfc lineNo，floor gfc lineNo;
        Dictionary<int, List<GFCElementModel>> floorEntityDict;//floor gfc lineNo, Construct Model;

        public GFCConvertEngine2(Dictionary<string, DeductGFCModel> modelList)
        {
            ModelList = modelList;
            globalId = 0;
            buildingStoreyGFCDict = new Dictionary<int, List<int>>();
            floorEntityDict = new Dictionary<int, List<GFCElementModel>>();
        }

        public void ToGFCEngine(THBimProject project, string docPath)
        {
            gfcDoc = ThGFC2Document.Create(docPath);
            try
            {
                PrjToGFC(project, docPath);
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

        private void PrjToGFC(THBimProject archiProj, string docPath)
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
                    floorEntityDict.Add(storeyId, new List<GFCElementModel>());

                    var storeyItemList = ModelList.Where(x => storeyPair.Value.ChildItems.Contains(x.Key)).Select(x => x.Value).ToList();

                    BuildArchiWallWithDoorWin(storeyId, storeyItemList);

                    BuildSlab(storeyId, storeyPair.Value, nextStoreyPair.Value, storeyName);

                    if (WithFitment == true)
                    {
                        BuildFurnish(storeyId, storeyItemList, storeyPair.Value, nextStoreyPair.Value, storeyName);
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


            var wallConstructs = floorEntityDict[storeyId].OfType<GFCWallModel>();//本层墙构建
            var windowConstructs = floorEntityDict[storeyId].OfType<GFCWindowModel>();//本层窗构建
            var doorConstructs = floorEntityDict[storeyId].OfType<GFCDoorModel>();//本层门构建

            //Step 2：创建本层的墙门窗图元并建立联系
            for (int i = 0; i < archiWallList.Count; i++)
            {
                var archiWall = archiWallList[i];
                var wallModel = archiWall.ToGfcArchiWallModel(gfcDoc, globalId);
                globalId++;
                if (wallModel != null)
                {
                    var wallConstruct = wallConstructs.First(o => o.WallThickness == wallModel.WallThickness);//因为构建也是我们自己建立的，所以该构建一定存在
                    wallConstruct.Primitives.Add(wallModel);// 建立 墙构建-墙图元 关系

                    var windows = windowList.Where(x => archiWall.ChildItems.Contains(x.UID)).ToList();
                    foreach (var win in windows)
                    {
                        var winModel = win.ToGfcWindowModel(gfcDoc, archiWall.GlobalZ, globalId);
                        globalId++;
                        var windowConstruct = windowConstructs.First(o => o.WindowHeight == winModel.WindowHeight && o.WindowLength == winModel.WindowLength);
                        windowConstruct.Primitives.Add(winModel);

                        wallModel.RelationElements.Add(winModel);// 建立 墙图元-窗图元 关系
                    }

                    var doors = doorList.Where(x => archiWall.ChildItems.Contains(x.UID)).ToList();
                    foreach (var door in doors)
                    {
                        var doorModel = door.ToGfcDoorModel(gfcDoc, archiWall.GlobalZ, globalId);
                        globalId++;
                        var doorConstruct = doorConstructs.First(o => o.DoorHeight == doorModel.DoorHeight && o.DoorLength == doorModel.DoorLength);
                        doorConstruct.Primitives.Add(doorModel);

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
                var matirial = "";
                int width = widthG.Key;
                var leftWidth = (int)width / 2;
                var wallModel = new GFCWallModel(gfcDoc, matirial, globalId, leftWidth, width);
                globalId++;
                floorEntityDict[storeyId].Add(wallModel);
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
                var windowModel = new GFCWindowModel(gfcDoc, "", globalId, windowLength, height);
                globalId++;
                floorEntityDict[storeyId].Add(windowModel);
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
                var doorModel = new GFCDoorModel(gfcDoc, "", globalId, doorLength, height);
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
                //var height = elevation - storeyElevation;
                if (elevation >= storeyElevation - aStorey.ZValue / 2 && elevation <= storeyElevation + nextAStorey.ZValue / 2)
                    return true;
                else
                    return false;

            }).ToList();
            BuildSlabModel(storeyId, slabs, storeyName);
            var slabConstructs = floorEntityDict[storeyId].OfType<GFCSlabModel>();

            foreach (var slab in slabs)
            {
                var slabModel = slab.ToGfcSlabModel(gfcDoc, globalId, storeyName);
                globalId++;

                var slabConstruct = slabConstructs.First(o => o.SlabThickness == slabModel.SlabThickness);
                slabConstruct.Primitives.Add(slabModel);
            }
        }

        private void BuildSlabModel(int storeyId, List<DeductGFCModel> slabList, string storeyName)
        {
            var group = slabList.GroupBy(x => (int)Math.Round(x.ZValue)).ToList();
            foreach (var thicknessG in group)
            {
                int thickness = thicknessG.Key;
                var slabModel = new GFCSlabModel(gfcDoc, "", globalId, storeyName, thickness);
                globalId++;
                floorEntityDict[storeyId].Add(slabModel);
            }
        }

        /// <summary>
        /// 装修
        /// </summary>
        private void BuildFurnish(int storeyId, List<DeductGFCModel> storeyItemList, DeductGFCModel aStorey, DeductGFCModel nextAStorey, string storeyName)
        {
            var slabPrimitives = floorEntityDict[storeyId].OfType<GFCSlabModel>().SelectMany(o => o.Primitives);
            var slabs = slabPrimitives.Select(p => p.Model).ToList();
            var wallPrimitives = floorEntityDict[storeyId].OfType<GFCWallModel>().SelectMany(o => o.Primitives);
            var walls = wallPrimitives.Select(p => p.Model).ToList();
            var rooms = storeyItemList.Where(x => x.ItemType == DeductType.Room).ToList();

            //房间构建
            //这里暂定所有的装修厚度都是一样的，之后可能根据做法分
            var roomThickness = 0;
            var roomName = "房间1";
            BuildRoomModel(storeyId, roomName);
            var roomConstructs = floorEntityDict[storeyId].OfType<GFCRoomModel>();
            //天棚构建
            var ceilingThickness = 10;
            BuildCeilingModel(storeyId, ceilingThickness);
            var ceilingConstructs = floorEntityDict[storeyId].OfType<GFCCeilingModel>();
            //底板构建
            var faceFloorFinishThickness = 0;
            BuildFaceFloorFinishModel(storeyId, faceFloorFinishThickness);
            var faceFloorFinishConstructs = floorEntityDict[storeyId].OfType<GFCFaceFloorFinishModel>();
            //墙面构建
            var wallFaceFinishName = "墙面1";
            var wallFaceFinishThickness = 0;
            BuildWallFaceFinishModel(storeyId, wallFaceFinishName, wallFaceFinishThickness);
            var wallFaceFinishConstructs = floorEntityDict[storeyId].OfType<GFCWallFaceFinishModel>();

            //房间构建关系
            //现在只有一个房间构建和一个天棚底板墙面构建，所以我们暂时全部为他们建立一个联系
            roomConstructs.First().RelationElements.Add(ceilingConstructs.First());
            roomConstructs.First().RelationElements.Add(faceFloorFinishConstructs.First());
            roomConstructs.First().RelationElements.Add(wallFaceFinishConstructs.First());
            //墙二维场景
            var wall2DSence = new ThNTSSpatialIndex(walls.Select(o => o.Outline)
                .OfType<NetTopologySuite.Geometries.Geometry>().ToList());
            foreach (var room in rooms)
            {
                //房间图元
                var roomModel = room.ToGfcRoomModel(gfcDoc, globalId, roomName, roomThickness);
                globalId++;
                var roomConstruct = roomConstructs.First(o => o.RoomName == roomModel.RoomName);
                roomConstruct.Primitives.Add(roomModel);

                //天棚图元
                var slabContainers = CheckRoomToSlab(room, slabs);
                if (slabContainers.Count() > 0)
                {
                    var ceilingModel = room.ToGfcCeilingModel(gfcDoc, globalId, ceilingThickness);
                    globalId++;
                    var ceilingConstruct = ceilingConstructs.First(o => o.CeilingThickness == ceilingModel.CeilingThickness);
                    ceilingConstruct.Primitives.Add(ceilingModel);

                    slabContainers.ForEach(slabContainer =>
                    {
                        var slabModel = slabPrimitives.FirstOrDefault(o => o.Model == slabContainer);
                        if (slabModel != null)
                        {
                            slabModel.RelationElements.Add(ceilingModel);
                        }
                    });
                }

                //底板图元
                var faceFloorFinishModel = room.ToGfcFaceFloorFinishModel(gfcDoc, globalId, faceFloorFinishThickness);
                globalId++;
                var faceFloorFinishConstruct = faceFloorFinishConstructs.First(o => o.FaceFloorFinishThickness == faceFloorFinishModel.FaceFloorFinishThickness);
                faceFloorFinishConstruct.Primitives.Add(faceFloorFinishModel);

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
                        var wallFaceFinishModel = WallModel.Model.ToGfcWallFaceFinishModel(gfcDoc, globalId, wallFaceFinishName, wallFaceFinishThickness, intersectLine);
                        globalId++;
                        var wallFaceFinishConstruct = wallFaceFinishConstructs.First(o => o.WallFaceFinishThickness == wallFaceFinishModel.WallFaceFinishThickness);
                        wallFaceFinishConstruct.Primitives.Add(wallFaceFinishModel);
                        WallModel.RelationElements.Add(wallFaceFinishModel);
                    }
                }
            }
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
        private void BuildRoomModel(int storeyId, string roomName)
        {
            var model = new GFCRoomModel(gfcDoc, "", globalId, roomName);
            globalId++;
            floorEntityDict[storeyId].Add(model);
        }


        private void BuildCeilingModel(int storeyId, int thickness)
        {
            var model = new GFCCeilingModel(gfcDoc, "", globalId, thickness);
            globalId++;
            floorEntityDict[storeyId].Add(model);
        }

        private void BuildFaceFloorFinishModel(int storeyId, int thickness)
        {
            var model = new GFCFaceFloorFinishModel(gfcDoc, "", globalId, thickness);
            globalId++;
            floorEntityDict[storeyId].Add(model);
        }

        private void BuildWallFaceFinishModel(int storeyId, string wallFaceFinishName, int thickness)
        {
            var model = new GFCWallFaceFinishModel(gfcDoc, "", globalId, wallFaceFinishName, thickness);
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
                var faceFloorFinishConstructs = floorEntityKeyValuePair.Value.OfType<GFCFaceFloorFinishModel>();
                var wallFaceFinishConstructs = floorEntityKeyValuePair.Value.OfType<GFCWallFaceFinishModel>();
                var roomConstructs = floorEntityKeyValuePair.Value.OfType<GFCRoomModel>();

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
                foreach (var roomConstruct in roomConstructs)
                {
                    gfcDoc.AddRelDefinesByElement(roomConstruct.ID, roomConstruct.Primitives.Select(o => o.ID).ToList());//建立【房间构建/房间图元】关系
                    gfcDoc.AddRelAggregate(roomConstruct.ID, roomConstruct.RelationElements.Select(o => o.ID).ToList());//建立【房间构建/天棚+底板+墙面构建】关系    
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
    }
}
