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
        public static int ToGfcProject(ThGFC2Document gfcDoc, int globalId, string name)
        {
            var project = new NGfc2Project();
            project.setID(globalId);
            project.setName(gfcDoc.AddGfc2String(name));
            project.setCode(gfcDoc.AddGfc2String(""));
            project.setProjectType(gfcDoc.AddGfc2String(""));
            project.setStructureType(NGfc2StructureType.ST_FRAME_SHEARWALL);
            //project.setFDType(gfcDoc.AddGfc2String("10"));
            //project.setArchiFeature(gfcDoc.AddGfc2String(""));
            //project.setBelowGroundFloorCount(0);
            //var floorcount = site.SiteBuildings.ElementAt(0).Value.BuildingStoreys.Count();
            //project.setAboveGroundFloorCount(floorcount);
            //project.setEavesHeight(0);
            //project.setFloorArea(0);
            project.setAseismicGrade(NGfc2AseismicGrade.Grade4);
            project.setProtectedIntensity(NGfc2ProtectedIntensity.Six);
            //projcet.setQuantities();
            //project.setGroundElev(0);

            var prjId = gfcDoc.AddEntity(project);

            //var prjSetting = new NGfc2ProjectSetting();
            //prjSetting.setBQCalcRuleName(gfcDoc.AddGfc2String(site.Name));
            //var prjSettingId = gfcDoc.AddEntity(prjSetting);

            //var edInfo = new NGfc2EditingInfo();
            //var timeS = System.DateTime.Now.ToString("yyyy-MM-dd");
            //edInfo.setDate(gfcDoc.AddGfc2String(timeS));
            //edInfo.setOwner(gfcDoc.AddGfc2String("TianHua"));
            //var edInfoId = gfcDoc.AddEntity(edInfo);

            return prjId;
        }

        public static int ToGfcBuilding(ThGFC2Document gfcDoc, int globalId, string name)
        {
            var b = new NGfc2Building();
            b.setID(globalId);
            b.setName(gfcDoc.AddGfc2String(name));
            var id = gfcDoc.AddEntity(b);

            return id;
        }

        public static int ToGfcStorey(this DeductGFCModel storey, ThGFC2Document gfcDoc, int globalId, string storeyName, int storeyNumber)
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

            return id;
        }

        public static int ToGfcArchiWall(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, double leftWidth, int width, string btmElev, string topElev)
        {
            var brickWall = new NGfc2BrickNormalWall();
            brickWall.setID(globalId);//必须要！
            brickWall.setName(gfcDoc.AddGfc2String(name));
            brickWall.setMaterial(gfcDoc.AddGfc2String(""));
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

            return id;
        }

        public static int ToGfcWindow(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, double windowLength, double windowHeight, double aboveFloorHeight)
        {
            var window = new NGfc2PointWindow();
            window.setID(globalId);
            window.setName(gfcDoc.AddGfc2String(name));
            if (shapeId != -1)
            {
                window.setShape(shapeId);
            }

            if (shapeId == -1)
            {
                var sectionId = gfcDoc.AddGfc2RectangleSection(windowLength, windowHeight);
                window.setSection(sectionId);
            }

            window.setFrameThickness(0);
            window.setFrameOffset(0);
            window.setAboveFloorHeight(aboveFloorHeight);
            window.setObliqueWithWall(true);
            var id = gfcDoc.AddEntity(window);

            return id;
        }

        public static int ToGfcDoor(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, int doorLength, int doorHeight, double aboveFloorHeight)
        {
            var door = new NGfc2PointDoor();
            door.setID(globalId);
            door.setName(gfcDoc.AddGfc2String(name));
            if (shapeId != -1)
            {
                door.setShape(shapeId);
            }

            if (shapeId == -1)
            {
                var sectionId = gfcDoc.AddGfc2RectangleSection(doorLength, doorHeight);
                door.setSection(sectionId);
            }

            door.setFrameThickness(0);
            door.setFrameOffset(0);
            door.setAboveFloorHeight(aboveFloorHeight);
            door.setObliqueWithWall(true);
            var id = gfcDoc.AddEntity(door);

            return id;
        }

        public static int ToGfcSlab(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, double thickness)
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

            return id;
        }

        public static int ToGfcWallFaceFinish(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, int thickness, string btmElev, string topElev, double axisOffset)
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

            return id;
        }

        public static int ToGfcCeiling(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, double thickness)
        {
            var id = -1;
            var ceiling = new NGfc2Ceiling();
            ceiling.setID(globalId);
            ceiling.setName(gfcDoc.AddGfc2String(name));
            ceiling.setThickness(thickness);

            if (shapeId != -1)
            {
                ceiling.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(ceiling);

            return id;
        }

        public static int ToGfcFaceFloorFinish(ThGFC2Document gfcDoc, int globalId, string name, int shapeId, double thickness, string topElev)
        {
            var id = -1;
            var faceFloorFinish = new NGfc2FaceFloorFinish();
            faceFloorFinish.setID(globalId);
            faceFloorFinish.setName(gfcDoc.AddGfc2String(name));
            faceFloorFinish.setThickness(thickness);
            faceFloorFinish.setTopElev(gfcDoc.AddGfc2String(topElev));
            faceFloorFinish.setFloorThickness(120);

            if (shapeId != -1)
            {
                faceFloorFinish.setShape(shapeId);
            }

            id = gfcDoc.AddEntity(faceFloorFinish);

            return id;
        }
    }
}
