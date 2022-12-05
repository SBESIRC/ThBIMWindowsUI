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
        public static int ToGfc(this THBimProject prj, ThGFC2Document gfcDoc)
        {
            var site = prj.ProjectSite;

            var project = new NGfc2Project();
            project.setName(gfcDoc.AddGfc2String(prj.ProjectIdentity));
            project.setStructureType(NGfc2StructureType.ST_FRAME_SHEARWALL);
            var floorcount = site.SiteBuildings.ElementAt(0).Value.BuildingStoreys.Count();
            project.setAboveGroundFloorCount(floorcount);
            project.setAseismicGrade(NGfc2AseismicGrade.Grade4);
            var prjId = gfcDoc.AddEntity(project);

            var prjSetting = new NGfc2ProjectSetting();
            prjSetting.setBQCalcRuleName(gfcDoc.AddGfc2String(site.Name));
            var prjSettingId = gfcDoc.AddEntity(prjSetting);

            var edInfo = new NGfc2EditingInfo();
            var timeS = System.DateTime.Now.ToString("yyyy-MM-dd");
            edInfo.setDate(gfcDoc.AddGfc2String(timeS));
            edInfo.setOwner(gfcDoc.AddGfc2String("TianHua"));
            var edInfoId = gfcDoc.AddEntity(edInfo);

            return prjSettingId;
        }

        public static int ToGfc(this THBimBuilding building, ThGFC2Document gfcDoc)
        {
            var b = new NGfc2Building();
            b.setID(0);
            var id = gfcDoc.AddEntity(b);
            return id;
        }

        public static int ToGfc(this THBimStorey storey, ThGFC2Document gfcDoc)
        {
            var stoeryG = new NGfc2Floor();
            stoeryG.setStructuralElevation(storey.Elevation);
            var first = storey.Id == 0 ? true : false;
            stoeryG.setFirstFloorFlag(first);
            stoeryG.setHeight(storey.LevelHeight);

            var id = gfcDoc.AddEntity(stoeryG);
            return id;
        }

        /// <summary>
        /// 这里确定拉伸体setStartPtHeight setEndPtHeight 是不是从上往下拉体
        /// </summary>
        /// <param name="wall"></param>
        /// <param name="gfcDoc"></param>
        /// <param name="floorHightMatrix"></param>
        /// <returns></returns>
        public static int ToGfc(this THBimWall wall, ThGFC2Document gfcDoc, XbimMatrix3D floorHightMatrix)
        {
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
                var pt0 = pts[(int)geom.Outline.Shell.Segments[0].Index[0]].Point3D2XBimPoint();
                var pt1 = pts[(int)geom.Outline.Shell.Segments[1].Index[0]].Point3D2XBimPoint();
                var pt2 = pts[(int)geom.Outline.Shell.Segments[2].Index[0]].Point3D2XBimPoint();
                var pt3 = pts[(int)geom.Outline.Shell.Segments[3].Index[0]].Point3D2XBimPoint();

                if (pt0.PointDistanceToPoint(pt1) <= pt1.PointDistanceToPoint(pt2))
                {
                    stPt = pt0.GetCenter(pt1);
                    endPt = pt2.GetCenter(pt3);
                    width = (int)pt0.PointDistanceToPoint(pt1);
                    leftWidth = (int)width / 2;
                }
                else
                {
                    stPt = pt1.GetCenter(pt2);
                    endPt = pt3.GetCenter(pt0);
                    width = (int)pt1.PointDistanceToPoint(pt2);
                    leftWidth = (int)width / 2;
                }
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

            var brickWall = new NGfc2BrickNormalWall();
            brickWall.setMaterial(gfcDoc.AddGfc2String(wall.Material));
            //brickWall.setInnerOuterFlag(NGfc2InnerOuterFlag.In);
            brickWall.setMortarType(gfcDoc.AddGfc2String("标准砖,混合砂浆,M5"));
            brickWall.setStartPtTopElev(gfcDoc.AddGfc2String("层底标高"));
            brickWall.setStartPtBtmElev(gfcDoc.AddGfc2String("层顶标高"));
            brickWall.setEndPtTopElev(gfcDoc.AddGfc2String("层底标高"));
            brickWall.setEndPtBtmElev(gfcDoc.AddGfc2String("层顶标高"));
            brickWall.setShape(shapeId);
            brickWall.setAxisOffset(leftWidth);
            brickWall.setThickness(width);
            var id = gfcDoc.AddEntity(brickWall);
            return id;
        }

    }
}
