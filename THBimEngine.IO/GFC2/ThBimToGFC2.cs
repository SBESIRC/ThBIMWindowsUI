using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using glodon.objectbufnet;
using Xbim.Common.Geometry;

using THBimEngine.Domain;

namespace THBimEngine.IO.GFC2
{
    public static class ThBimToGFC2
    {
        public static int ToGfc(this THBimProject prj, ThGFC2Document gfcDoc, ref int globelID)
        {
            var site = prj.ProjectSite;

            var project = new NGfc2Project();
            project.setID(globelID);
            project.setName(gfcDoc.AddGfc2String(prj.ProjectIdentity));
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

            globelID++;

            return prjId;
        }

        public static int ToGfc(this THBimBuilding building, ThGFC2Document gfcDoc, ref int globelID)
        {
            var b = new NGfc2Building();
            b.setID(globelID);
            b.setName(gfcDoc.AddGfc2String("building" + building.Name));
            var id = gfcDoc.AddEntity(b);
            globelID++;

            return id;
        }

        public static int ToGfc(this THBimStorey storey, ThGFC2Document gfcDoc, ref int globelID)
        {
            var stoeryG = new NGfc2Floor();
            stoeryG.setID(globelID);
            stoeryG.setName(gfcDoc.AddGfc2String(storey.Name));
            stoeryG.setHeight(storey.LevelHeight / 1000);//单位米
            stoeryG.setStdFloorCount(1);
            stoeryG.setStructuralElevation(storey.Elevation / 1000);//单位米
            stoeryG.setStartFloorNo(1);
            stoeryG.setFloorArea(0);
            stoeryG.setSlabThickness(0);
            stoeryG.setRemark(gfcDoc.AddGfc2String(""));

            var id = gfcDoc.AddEntity(stoeryG);
            globelID++;
            return id;
        }

        /// <summary>
        /// 拉伸体是靠墙的底标高顶标高（setEndPtTopElev） string做出来的，且为绝对值，单位m
        /// </summary>
        /// <param name="wall"></param>
        /// <param name="gfcDoc"></param>
        /// <param name="floorHightMatrix"></param>
        /// <returns></returns>
        public static int ToGfc(this THBimWall wall, ThGFC2Document gfcDoc, XbimMatrix3D floorHightMatrix, ref int globelID, ref Dictionary<int, Tuple<int, List<int>>> entityModel)
        {
            var id = -1;
            //转换wall geometry 到 GFC lineShape
            var geom = wall.GeometryParam as GeometryStretch;
            int width = 0;
            int leftWidth = 0;
            var stPt = new XbimPoint3D(0, 0, 0);
            var endPt = new XbimPoint3D(0, 0, 0);
            var zS = geom.ZAxisLength;
            var zE = 0;
            if (geom.Outline != null && geom.Outline.Shell != null)
            {
                var pts = geom.Outline.Shell.Points;
                var pt0 = new XbimPoint3D();
                var pt1 = new XbimPoint3D();
                var pt2 = new XbimPoint3D();
                var pt3 = new XbimPoint3D();

                if (pts.Count > 3)
                {
                    pt0 = pts[(int)geom.Outline.Shell.Segments[0].Index[0]].Point3D2XBimPoint();
                    pt1 = pts[(int)geom.Outline.Shell.Segments[1].Index[0]].Point3D2XBimPoint();
                    pt2 = pts[(int)geom.Outline.Shell.Segments[2].Index[0]].Point3D2XBimPoint();
                    pt3 = pts[(int)geom.Outline.Shell.Segments[3].Index[0]].Point3D2XBimPoint();

                    if (pt0.PointDistanceToPoint(pt1) <= pt1.PointDistanceToPoint(pt2))
                    {
                        stPt = pt0.GetCenter(pt1);
                        endPt = pt2.GetCenter(pt3);
                        width = (int)Math.Round(pt0.PointDistanceToPoint(pt1));

                    }
                    else
                    {
                        stPt = pt1.GetCenter(pt2);
                        endPt = pt3.GetCenter(pt0);
                        width = (int)Math.Round(pt1.PointDistanceToPoint(pt2));

                    }
                }
                else if (pts.Count == 3)
                {
                    pt0 = pts[(int)geom.Outline.Shell.Segments[0].Index[0]].Point3D2XBimPoint();
                    pt1 = pts[(int)geom.Outline.Shell.Segments[0].Index[1]].Point3D2XBimPoint();

                    stPt = pt0;
                    endPt = pt1;

                    width = (int)Math.Round(pt0.PointDistanceToPoint(pt1));

                }
            }

            leftWidth = (int)width / 2;

            //写入构件
            var hasModel = entityModel.ContainsKey(width);
            if (hasModel == false)
            {
                var modelId = wall.ToGfcWallModel(gfcDoc, ref globelID, leftWidth, width);
                entityModel.Add(width, new Tuple<int, List<int>>(modelId, new List<int>()));
            }


            //写入
            var lineId = gfcDoc.AddGfc2Line2d(stPt, endPt);
            var locationId = gfcDoc.AddGfc2Coordinates3d(floorHightMatrix);
            var shape = new NGfc2LineShape();
            shape.setLocalCoordinate(locationId);
            shape.setWidth(width);
            shape.setLeftWidth(leftWidth);
            shape.setLine(lineId);
            shape.setE_S_Elevation(0);
            shape.setTilt(0);
            shape.setStartPtHeight(zS);
            shape.setEndPtHeight(zE);
            var shapeId = gfcDoc.AddEntity(shape);

            var btmElev = floorHightMatrix.OffsetZ;
            var topElev = floorHightMatrix.OffsetZ + geom.ZAxisLength;

            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();//单位m，而且这两个是控制实际高度的，不是用真正的几何体。。。。
            var topElevS = Math.Round(topElev / 1000, 2).ToString();


            id = ToGfcWall(gfcDoc, wall.Material, shapeId, leftWidth, width, btmElevS, topElevS, ref globelID);

            entityModel[width].Item2.Add(id);

            return id;
        }

        public static int ToGfcWallModel(this THBimWall wall, ThGFC2Document gfcDoc, ref int globelID, double leftWidth, int width)
        {
            var id = ToGfcWall(gfcDoc, wall.Material, -1, leftWidth, width, "层底标高", "层顶标高", ref globelID);
            return id;
        }

        public static int ToGfcWall(ThGFC2Document gfcDoc, string material, int shapeId, double leftWidth, int width, string btmElev, string topElev, ref int globelID)
        {
            var brickWall = new NGfc2BrickNormalWall();
            brickWall.setID(globelID);//必须要！
            brickWall.setName(gfcDoc.AddGfc2String("内墙100"));
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

            globelID++;
            return id;
        }

    }
}
