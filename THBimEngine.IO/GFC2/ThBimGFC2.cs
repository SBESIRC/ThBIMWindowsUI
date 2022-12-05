using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using glodon.objectbufnet;
using THBimEngine.Domain;

namespace THBimEngine.IO.GFC2
{
    public static class ThBimGFC2
    {
        public static void ToGfc(this THBimProject prj, ThGFCDocument gfcDoc)
        {
            var site = prj.ProjectSite;

            var project = new NGfc2Project();
            project.setName(gfcDoc.NewNGfc2String(prj.ProjectIdentity));
            project.setStructureType(NGfc2StructureType.ST_FRAME_SHEARWALL);
            var floorcount = site.SiteBuildings.ElementAt(0).Value.BuildingStoreys.Count();
            project.setAboveGroundFloorCount(floorcount);
            project.setAseismicGrade(NGfc2AseismicGrade.Grade4);
            var prjId = gfcDoc.writeEntity(project);

            var prjSetting = new NGfc2ProjectSetting();
            prjSetting.setBQCalcRuleName(gfcDoc.NewNGfc2String(site.Name));
            var prjSettingId = gfcDoc.writeEntity(prjSetting);

            var edInfo = new NGfc2EditingInfo();
            var timeS = System.DateTime.Now.ToString("yyyy-MM-dd");
            edInfo.setDate(gfcDoc.NewNGfc2String(timeS));
            edInfo.setOwner(gfcDoc.NewNGfc2String("TianHua"));
            var edInfoId = gfcDoc.writeEntity(edInfo);
        }


        public static void ToGfc(this THBimBuilding building, ThGFCDocument gfcDoc)
        {
            var b = new NGfc2Building();
            b.setID(0);
            var id = gfcDoc.writeEntity(b);
        }

        public static int ToGfc(this THBimStorey storey, ThGFCDocument gfcDoc)
        {
            var stoeryG = new NGfc2Floor();
            stoeryG.setStructuralElevation(storey.Elevation);
            var first = storey.Id == 0 ? true : false;
            stoeryG.setFirstFloorFlag(first);
            stoeryG.setHeight(storey.LevelHeight);

            var id = gfcDoc.writeEntity(stoeryG);
            return id;
        }
        public static int ToGfc(this THBimWall wall, ThGFCDocument gfcDoc)
        {
            var brickWall = new NGfc2BrickNormalWall();

            brickWall.setMaterial(gfcDoc.NewNGfc2String(wall.Material));
            brickWall.setInnerOuterFlag(NGfc2InnerOuterFlag.In);
            brickWall.setMortarType(gfcDoc.NewNGfc2String("标准砖,混合砂浆,M5"));
            brickWall.setStartPtTopElev(gfcDoc.NewNGfc2String("层底标高"));
            brickWall.setStartPtBtmElev(gfcDoc.NewNGfc2String("层顶标高"));
            brickWall.setEndPtTopElev(gfcDoc.NewNGfc2String("层底标高"));
            brickWall.setEndPtBtmElev(gfcDoc.NewNGfc2String("层顶标高"));

            var shape = new NGfc2LineShape();


            brickWall.setShape();
            brickWall.setAxisOffset();
            brickWall.setThickness();





            var id = gfcDoc.writeEntity(brickWall);
            return id;
        }


    }
}
