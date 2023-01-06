using System;
using System.Linq;

using Xbim.Common.Geometry;
using XbimXplorer.Deduct.Model;
using THBimEngine.IO.GFC2;
using NetTopologySuite.Geometries;
using THBimEngine.Domain;

namespace XbimXplorer.Deduct
{
    public static class THModelToGFC2
    {
        public static int ToGfcBuilding(this DeductGFCModel building, ThGFC2Document gfcDoc, ref int globalId)
        {
            var b = new NGfc2Building();
            b.setID(globalId);
            b.setName(gfcDoc.AddGfc2String("building" + globalId));
            var id = gfcDoc.AddEntity(b);
            globalId++;

            return id;
        }

        public static int ToGfcStorey(this DeductGFCModel storey, ThGFC2Document gfcDoc, ref int globalId, string storeyName, int storeyNumber)
        {
            var stoeryG = new NGfc2Floor();
            stoeryG.setID(globalId);
            stoeryG.setName(gfcDoc.AddGfc2String(storeyName));
            stoeryG.setHeight(storey.ZValue / 1000);//单位米
            stoeryG.setStdFloorCount(1);
            stoeryG.setStructuralElevation(storey.GlobalZ / 1000);//单位米
            stoeryG.setStartFloorNo(storeyNumber);
            stoeryG.setFloorArea(0);
            stoeryG.setSlabThickness(120);
            stoeryG.setRemark(gfcDoc.AddGfc2String(""));

            var id = gfcDoc.AddEntity(stoeryG);
            globalId++;
            return id;
        }

        public static GFCWallModel ToGfcArchiWallModel(this DeductGFCModel archiWall, ThGFC2Document gfcDoc, int globalId)
        {
            int width = (int)Math.Round(archiWall.Width);
            var leftWidth = (int)width / 2;
            var stPt = archiWall.CenterLine.P0;
            var endPt = archiWall.CenterLine.P1;

            var zS = archiWall.ZValue;
            var zE = 0;
            var globalLocation = new XbimMatrix3D(new XbimVector3D(0, 0, archiWall.GlobalZ));
            var matirial = "";

            //写入
            var lineId = gfcDoc.AddGfc2Line2d(stPt, endPt);
            var locationId = gfcDoc.AddGfc2Coordinates3d(globalLocation);
            var shapeId = gfcDoc.AddGfc2LineShape(locationId, width, leftWidth, lineId, zS, zE);

            var btmElev = archiWall.GlobalZ;
            var topElev = archiWall.GlobalZ + zS;

            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();//单位m，而且这两个是控制实际高度的，不是用真正的几何体。。。。
            var topElevS = Math.Round(topElev / 1000, 2).ToString();

            var wallModel = new GFCWallModel(gfcDoc, matirial, archiWall, globalId, shapeId, leftWidth, width, btmElevS, topElevS);

            return wallModel;
        }

        public static int ToGfcArchiWall(this DeductGFCModel archiWall, ThGFC2Document gfcDoc, ref int globalId)
        {
            var id = -1;

            int width = (int)Math.Round(archiWall.Width);
            var leftWidth = (int)width / 2;
            var stPt = archiWall.CenterLine.P0;
            var endPt = archiWall.CenterLine.P1;

            var zS = archiWall.ZValue;
            var zE = 0;
            var globalLocation = new XbimMatrix3D(new XbimVector3D(0, 0, archiWall.GlobalZ));
            var matirial = "";

            ////写入构件
            //var hasModel = entityModel.ContainsKey(width);
            //if (hasModel == false)
            //{
            //    var modelId = ToGfcArchiWallModel(gfcDoc, matirial, ref globalId, leftWidth, width);
            //    entityModel.Add(width, new Tuple<int, List<int>>(modelId, new List<int>()));
            //}

            //写入
            var lineId = gfcDoc.AddGfc2Line2d(stPt, endPt);
            var locationId = gfcDoc.AddGfc2Coordinates3d(globalLocation);
            var shapeId = gfcDoc.AddGfc2LineShape(locationId, width, leftWidth, lineId, zS, zE);

            var btmElev = archiWall.GlobalZ;
            var topElev = archiWall.GlobalZ + zS;

            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();//单位m，而且这两个是控制实际高度的，不是用真正的几何体。。。。
            var topElevS = Math.Round(topElev / 1000, 2).ToString();


            id = ToGfcArchiWall(gfcDoc, matirial, shapeId, leftWidth, width, btmElevS, topElevS, ref globalId);

            //entityModel[width].Item2.Add(id);

            return id;
        }

        public static int ToGfcArchiWallModel(ThGFC2Document gfcDoc, string matirial, ref int globalId, double leftWidth, int width)
        {
            var id = ToGfcArchiWall(gfcDoc, matirial, -1, leftWidth, width, "层底标高", "层顶标高", ref globalId);
            return id;
        }

        public static int ToGfcArchiWall(ThGFC2Document gfcDoc, string material, int shapeId, double leftWidth, int width, string btmElev, string topElev, ref int globalId)
        {
            var brickWall = new NGfc2BrickNormalWall();
            brickWall.setID(globalId);//必须要！
            brickWall.setName(gfcDoc.AddGfc2String(String.Format("内墙{0}", width)));
            brickWall.setMaterial(gfcDoc.AddGfc2String(material));
            brickWall.setOriginalMaterial(gfcDoc.AddGfc2String("标准砖,混合砂浆,M5"));
            brickWall.setInnerOuterFlag(NGfc2InnerOuterFlag.In);
            brickWall.setMortarType(gfcDoc.AddGfc2String("标准砖,混合砂浆,M5"));
            brickWall.setStartPtBtmElev(gfcDoc.AddGfc2String(btmElev));
            brickWall.setStartPtTopElev(gfcDoc.AddGfc2String(topElev));
            brickWall.setEndPtBtmElev(gfcDoc.AddGfc2String(btmElev));
            brickWall.setEndPtTopElev(gfcDoc.AddGfc2String(topElev));

            if (shapeId != -1)
            {
                brickWall.setShape(shapeId);
            }
            brickWall.setAxisOffset(leftWidth);
            brickWall.setThickness(width);

            var id = gfcDoc.AddEntity(brickWall);

            globalId++;
            return id;
        }

        public static GFCWindowModel ToGfcWindowModel(this DeductGFCModel window, ThGFC2Document gfcDoc, double wallGlobalZ, int globalId)
        {
            var windowLength = (int)Math.Round(window.CenterLine.Length);
            var height = (int)Math.Round(window.ZValue);
            var aboveFloorHeight = window.GlobalZ - wallGlobalZ;

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wallGlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location); //墙的高度

            var interPt = window.CenterLine.MidPoint;
            var interPtId = gfcDoc.AddGfc2Vector2d(interPt.X, interPt.Y); //2d的投影，中点
            var baseInterPtId = gfcDoc.AddGfc2Vector2d(0, -height / 2); //高度,下边界为原点
            var polyId = gfcDoc.AddSimpolyPolygon(windowLength, height);//长,高/2的四边形
            var shapeId = gfcDoc.AddSectionPointShape(localCoordinateId, interPtId, baseInterPtId, polyId);
            var model = new GFCWindowModel(gfcDoc, "", window, shapeId, false, globalId, windowLength, height, aboveFloorHeight);
            return model;
        }

        public static int ToGfcWindow(this DeductGFCModel window, ThGFC2Document gfcDoc, double wallGlobalZ, ref int globalId)
        {
            var windowLength = (int)Math.Round(window.CenterLine.Length);
            var height = (int)Math.Round(window.ZValue);
            var aboveFloorHeight = window.GlobalZ - wallGlobalZ;

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wallGlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location); //墙的高度

            var interPt = window.CenterLine.MidPoint;
            var interPtId = gfcDoc.AddGfc2Vector2d(interPt.X, interPt.Y); //2d的投影，中点
            var baseInterPtId = gfcDoc.AddGfc2Vector2d(0, -height / 2); //高度,下边界为原点
            var polyId = gfcDoc.AddSimpolyPolygon(windowLength, height);//长,高/2的四边形
            var shapeId = gfcDoc.AddSectionPointShape(localCoordinateId, interPtId, baseInterPtId, polyId);

            var id = ToGfcWindow(gfcDoc, shapeId, false, windowLength, height, aboveFloorHeight, ref globalId);
            return id;
        }
        public static int ToGfcWindowModel(ThGFC2Document gfcDoc, double windowLength, double height, ref int globalId)
        {
            var id = ToGfcWindow(gfcDoc, -1, true, windowLength, height, 0, ref globalId);
            return id;
        }

        public static int ToGfcWindow(ThGFC2Document gfcDoc, int shapeId, bool isModel, double windowLength, double height, double aboveFloorHeight, ref int globalId)
        {
            var window = new NGfc2PointWindow();
            window.setID(globalId);
            window.setName(gfcDoc.AddGfc2String(""));
            if (shapeId != -1)
            {
                window.setShape(shapeId);
            }
            if (isModel == true)
            {
                var sectionId = gfcDoc.AddGfc2RectangleSection(windowLength, height);
                window.setSection(sectionId);
            }

            window.setFrameThickness(0);
            window.setFrameOffset(0);
            window.setAboveFloorHeight(aboveFloorHeight);
            window.setObliqueWithWall(true);
            var id = gfcDoc.AddEntity(window);
            globalId++;

            return id;
        }

        public static GFCDoorModel ToGfcDoorModel(this DeductGFCModel door, ThGFC2Document gfcDoc, double wallGlobalZ, int globalId)
        {
            var doorLength = (int)Math.Round(door.CenterLine.Length);
            var height = (int)Math.Round(door.ZValue);
            var aboveFloorHeight = door.GlobalZ - wallGlobalZ;

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wallGlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location); //高度

            var interPt = door.CenterLine.MidPoint;
            var interPtId = gfcDoc.AddGfc2Vector2d(interPt.X, interPt.Y); //2d的投影，中点
            var baseInterPtId = gfcDoc.AddGfc2Vector2d(0, -height / 2); //高度,下边界为原点
            var polyId = gfcDoc.AddSimpolyPolygon(doorLength, height);//长,高/2的四边形
            var shapeId = gfcDoc.AddSectionPointShape(localCoordinateId, interPtId, baseInterPtId, polyId);

            var doorModel = new GFCDoorModel(gfcDoc, "", door, shapeId, false, globalId, doorLength, height, aboveFloorHeight);
            return doorModel;
        }
        public static int ToGfcDoor(this DeductGFCModel door, ThGFC2Document gfcDoc, double wallGlobalZ, ref int globalId)
        {
            var doorLength = (int)Math.Round(door.CenterLine.Length);
            var height = (int)Math.Round(door.ZValue);
            var aboveFloorHeight = door.GlobalZ - wallGlobalZ;

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wallGlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location); //高度

            var interPt = door.CenterLine.MidPoint;
            var interPtId = gfcDoc.AddGfc2Vector2d(interPt.X, interPt.Y); //2d的投影，中点
            var baseInterPtId = gfcDoc.AddGfc2Vector2d(0, -height / 2); //高度,下边界为原点
            var polyId = gfcDoc.AddSimpolyPolygon(doorLength, height);//长,高/2的四边形
            var shapeId = gfcDoc.AddSectionPointShape(localCoordinateId, interPtId, baseInterPtId, polyId);

            var id = ToGfcDoor(gfcDoc, shapeId, false, doorLength, height, aboveFloorHeight, ref globalId);
            return id;
        }
        public static int ToGfcDoorModel(ThGFC2Document gfcDoc, double doorLength, double height, ref int globalId)
        {
            var id = ToGfcDoor(gfcDoc, -1, true, doorLength, height, 0, ref globalId);
            return id;
        }

        public static int ToGfcDoor(ThGFC2Document gfcDoc, int shapeId, bool isModel, double doorLength, double height, double aboveFloorHeight, ref int globalId)
        {
            var door = new NGfc2PointDoor();
            door.setID(globalId);
            door.setName(gfcDoc.AddGfc2String(""));
            if (shapeId != -1)
            {
                door.setShape(shapeId);
            }
            if (isModel == true)
            {
                var sectionId = gfcDoc.AddGfc2RectangleSection(doorLength, height);
                door.setSection(sectionId);
            }

            door.setFrameThickness(0);
            door.setFrameOffset(0);
            door.setAboveFloorHeight(aboveFloorHeight);
            door.setObliqueWithWall(true);
            var id = gfcDoc.AddEntity(door);
            globalId++;

            return id;
        }

        public static int ToGfcSlabModel(ThGFC2Document gfcDoc, ref int globalId, string name, double thickness)
        {
            var id = -1;
            id = ToGfcSlab(gfcDoc, ref globalId, name, thickness, -1);

            return id;
        }

        public static GFCSlabModel ToGfcSlabModel(this DeductGFCModel slab, ThGFC2Document gfcDoc, int globalId, string name)
        {
            var thickness = (int)Math.Round(slab.ZValue);

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, slab.GlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = slab.Outline.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);

            var model = new GFCSlabModel(gfcDoc, "", slab, shapeId, globalId, thickness, name);
            return model;
        }

        public static int ToGfcSlab(this DeductGFCModel slab, ThGFC2Document gfcDoc, ref int globalId, string name)
        {
            var thickness = Math.Round(slab.ZValue);

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, slab.GlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = slab.Outline.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);


            var id = ToGfcSlab(gfcDoc, ref globalId, name, thickness, shapeId);
            return id;
        }

        public static int ToGfcSlab(ThGFC2Document gfcDoc, ref int globalId, string name, double thickness, int shapeId)
        {
            var id = -1;
            var slab = new NGfc2FaceSlab();
            slab.setID(globalId);
            slab.setName(gfcDoc.AddGfc2String(name));

            //slab.setConcType();
            //slab.setConcGrade();
            //slab.setMaterial();
            slab.setSlabType(NGfc2SlabType.YLB);
            slab.setOriginalMaterial(gfcDoc.AddGfc2String("现浇混凝土,碎石混凝土 坍落度30-50 石子最大粒径40mm,C20,"));
            slab.setThickness(thickness);
            slab.setTopElev(gfcDoc.AddGfc2String("层顶标高"));

            if (shapeId != -1)
            {
                slab.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(slab);
            globalId++;
            return id;
        }


        public static int ToGfcWallFaceFinishModel(ThGFC2Document gfcDoc, ref int globalId, string name, int thickness)
        {
            var id = -1;
            id = ToGfcWallFaceFinish(gfcDoc, ref globalId, name, thickness, -1, "墙底标高", "墙顶标高", 50);

            return id;
        }

        public static GFCWallFaceFinishModel ToGfcWallFaceFinishModel(this DeductGFCModel wall, ThGFC2Document gfcDoc, int globalId, string name, int wallFaceFinishThickness, LineString wallFaceFinishLine)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wall.GlobalZ));
            var extendLine = ExtendLine(wallFaceFinishLine, -1);
            var stPt = extendLine.Item1;
            var endPt = extendLine.Item2;
            var lineId = gfcDoc.AddGfc2Line2d(stPt, endPt);
            var locationId = gfcDoc.AddGfc2Coordinates3d(location);
            var zS = wall.ZValue;
            var zE = 0;
            var btmElev = wall.GlobalZ;
            var topElev = wall.GlobalZ + zS;
            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();//单位m，而且这两个是控制实际高度的，不是用真正的几何体。。。。
            var topElevS = Math.Round(topElev / 1000, 2).ToString();
            var shapeId = gfcDoc.AddGfc2LineShape(locationId, 0, 0, lineId, zS, zE);
            var isLeft = stPt.IsLeftPt(new XbimPoint3D(wall.CenterLine.P0.X, wall.CenterLine.P0.Y, 0), new XbimPoint3D(wall.CenterLine.P1.X, wall.CenterLine.P1.Y, 0));

            //这里有一个很crazy的地方，axisOffset是距墙的左面距离值，但是当axisOffset值为0时，它会布置在墙的右面。
            var model = new GFCWallFaceFinishModel(gfcDoc,"", shapeId, globalId, name, wallFaceFinishThickness, btmElevS, topElevS, isLeft ? 50 : 0);
            return model;
        }
        public static int ToGfcWallFaceFinish(this DeductGFCModel wall, ThGFC2Document gfcDoc, ref int globalId, string name, LineString wallFaceFinishLine)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wall.GlobalZ));
            var extendLine = ExtendLine(wallFaceFinishLine, -1);
            var stPt = extendLine.Item1;
            var endPt = extendLine.Item2;
            var lineId = gfcDoc.AddGfc2Line2d(stPt, endPt);
            var locationId = gfcDoc.AddGfc2Coordinates3d(location);
            var zS = wall.ZValue;
            var zE = 0;
            var btmElev = wall.GlobalZ;
            var topElev = wall.GlobalZ + zS;
            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();//单位m，而且这两个是控制实际高度的，不是用真正的几何体。。。。
            var topElevS = Math.Round(topElev / 1000, 2).ToString();
            var shapeId = gfcDoc.AddGfc2LineShape(locationId, 0, 0, lineId, zS, zE);
            var isLeft = stPt.IsLeftPt(new XbimPoint3D(wall.CenterLine.P0.X, wall.CenterLine.P0.Y, 0), new XbimPoint3D(wall.CenterLine.P1.X, wall.CenterLine.P1.Y, 0));

            //这里有一个很crazy的地方，axisOffset是距墙的左面距离值，但是当axisOffset值为0时，它会布置在墙的右面。
            var id = ToGfcWallFaceFinish(gfcDoc, ref globalId, name, 0, shapeId, btmElevS, topElevS, isLeft ? 50 : 0);
            return id;
        }

        private static Tuple<XbimPoint3D, XbimPoint3D> ExtendLine(LineString line, double distance)
        {
            var stPt = new XbimPoint3D(line.StartPoint.X, line.StartPoint.Y, 0);
            var endPt = new XbimPoint3D(line.EndPoint.X, line.EndPoint.Y, 0);
            var direction = (endPt - stPt).Normalized();
            var newSPt = stPt - direction * distance;
            var newEPt = endPt + direction * distance;
            return (new XbimPoint3D(Math.Round(newSPt.X,2), Math.Round(newSPt.Y, 2),0), new XbimPoint3D(Math.Round(newEPt.X, 2), Math.Round(newEPt.Y, 2), 0)).ToTuple();
        }

        public static int ToGfcWallFaceFinish(ThGFC2Document gfcDoc, ref int globalId, string name, int thickness, int shapeId ,string btmElev, string topElev,double axisOffset)
        {
            var id = -1;
            var wallFaceFinish = new NGfc2LineWallFaceFinish();
            wallFaceFinish.setID(globalId);
            wallFaceFinish.setName(gfcDoc.AddGfc2String(name));
            wallFaceFinish.setStartPtTopElev(gfcDoc.AddGfc2String(topElev));
            wallFaceFinish.setStartPtBottomElev(gfcDoc.AddGfc2String(btmElev));
            wallFaceFinish.setEndPtTopElev(gfcDoc.AddGfc2String(topElev));
            wallFaceFinish.setEndPtBottomElev(gfcDoc.AddGfc2String(btmElev));
            wallFaceFinish.setAxisOffset(axisOffset);
            //wallFaceFinish.setAxisOffset
            //wallFaceFinish.setOriginalMaterial(gfcDoc.AddGfc2String("现浇混凝土,碎石混凝土 坍落度30-50 石子最大粒径40mm,C20,"));
            wallFaceFinish.setThickness(thickness);
            //wallFaceFinish.setTopElev(gfcDoc.AddGfc2String("层顶标高"));

            if (shapeId != -1)
            {
                wallFaceFinish.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(wallFaceFinish);
            globalId++;
            return id;
        }

        public static int ToGfcCeilingModel(ThGFC2Document gfcDoc, ref int globalId, double thickness)
        {
            var id = -1;
            id = ToGfcCeiling(gfcDoc, ref globalId, thickness, -1);
            return id;
        }

        public static GFCCeilingModel ToGfcCeilingModel(this DeductGFCModel room, ThGFC2Document gfcDoc, int globalId, int thickness)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, room.GlobalZ + room.ZValue));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);

            var model = new GFCCeilingModel(gfcDoc, "", room, shapeId, globalId, thickness);
            return model;
        }

        public static int ToGfcCeiling(this DeductGFCModel room, ThGFC2Document gfcDoc, ref int globalId, double thickness)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, room.GlobalZ + room.ZValue));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);

            var id = ToGfcCeiling(gfcDoc, ref globalId, thickness, shapeId);
            return id;
        }

        public static int ToGfcCeiling(ThGFC2Document gfcDoc, ref int globalId, double thickness, int shapeId)
        {
            var id = -1;
            var ceiling = new NGfc2Ceiling();
            ceiling.setID(globalId);
            ceiling.setName(gfcDoc.AddGfc2String("天棚"));
            ceiling.setThickness(thickness);

            if (shapeId != -1)
            {
                ceiling.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(ceiling);
            globalId++;
            return id;
        }

        public static int ToGfcFaceFloorFinishModel(ThGFC2Document gfcDoc, ref int globalId, double thickness)
        {
            var id = -1;
            var topElev = "层底标高";
            id = ToGfcFaceFloorFinish(gfcDoc, ref globalId, thickness, topElev, -1);
            return id;
        }

        public static GFCFaceFloorFinishModel ToGfcFaceFloorFinishModel(this DeductGFCModel room, ThGFC2Document gfcDoc, int globalId, int thickness)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, 0));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);
            var topElev = Math.Round(room.GlobalZ / 1000, 2).ToString();
            var model = new GFCFaceFloorFinishModel(gfcDoc, "", shapeId, globalId, thickness, topElev);
            return model;
        }

        public static int ToGfcFaceFloorFinish(this DeductGFCModel room, ThGFC2Document gfcDoc, ref int globalId, double thickness)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, 0));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);
            var topElev = Math.Round(room.GlobalZ / 1000, 2).ToString();

            var id = ToGfcFaceFloorFinish(gfcDoc, ref globalId, thickness, topElev, shapeId);
            return id;
        }

        public static int ToGfcFaceFloorFinish(ThGFC2Document gfcDoc, ref int globalId, double thickness, string topElev, int shapeId)
        {
            var id = -1;
            var faceFloorFinish = new NGfc2FaceFloorFinish();
            faceFloorFinish.setID(globalId);
            faceFloorFinish.setName(gfcDoc.AddGfc2String("地面1"));
            faceFloorFinish.setThickness(thickness);
            faceFloorFinish.setTopElev(gfcDoc.AddGfc2String(topElev));
            faceFloorFinish.setFloorThickness(120);

            if (shapeId != -1)
            {
                faceFloorFinish.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(faceFloorFinish);
            globalId++;
            return id;
        }

        public static GFCRoomModel ToGfcRoomModel(this DeductGFCModel room, ThGFC2Document gfcDoc, int globalId, string name, double thickness)
        {
            var btmElev = room.GlobalZ;
            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, 0));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);
            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);
            var model = new GFCRoomModel(gfcDoc,"", room,shapeId, globalId, name, btmElevS);
            return model;
        }
        public static int ToGfcRoom(this DeductGFCModel room, ThGFC2Document gfcDoc, ref int globalId, string name,double thickness)
        {
            var btmElev = room.GlobalZ;
            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, 0));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);
            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);

            var id = ToGfcRoom(gfcDoc, ref globalId, name, btmElevS, shapeId);
            return id;
        }
        public static int ToGfcRoomModel(ThGFC2Document gfcDoc, ref int globalId, string name)
        {
            var id = ToGfcRoom(gfcDoc, ref globalId, name, "层底标高", -1);
            return id;
        }
        public static int ToGfcRoom(ThGFC2Document gfcDoc, ref int globalId, string name, string btmElev, int shapeId)
        {
            var id = -1;
            var faceRoom = new NGfc2FaceRoom();
            faceRoom.setID(globalId);
            faceRoom.setName(gfcDoc.AddGfc2String(name));
            faceRoom.setBottomElev(gfcDoc.AddGfc2String(btmElev));

            if (shapeId != -1)
            {
                faceRoom.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(faceRoom);
            globalId++;
            return id;
        }
    }
}
