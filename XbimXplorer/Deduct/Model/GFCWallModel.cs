using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common.Geometry;

using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCWallModel : GFCElementModel
    {
        //构件属性
        public int WallThickness { get; set; }

        public GFCWallModel(ThGFC2Document gfcDoc, int globalId, string name, int width) : base(gfcDoc, globalId, name)
        {
            WallThickness = width;
            IsConstruct = true;

            var leftWidth = (int)width / 2;

            ID = THModelToGFC2.ToGfcArchiWall(gfcDoc, globalId, name, -1, leftWidth, WallThickness, "层底标高", "层顶标高");
        }

        public GFCWallModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel archiWall) : base(gfcDoc, globalId, name)
        {
            int width = (int)Math.Round(archiWall.Width);
            var leftWidth = (int)width / 2;

            var stPt = archiWall.CenterLine.P0;
            var endPt = archiWall.CenterLine.P1;
            var zS = archiWall.ZValue;
            var zE = 0;
            var globalLocation = new XbimMatrix3D(new XbimVector3D(0, 0, archiWall.GlobalZ));

            //写入
            var lineId = gfcDoc.AddGfc2Line2d(stPt, endPt);
            var locationId = gfcDoc.AddGfc2Coordinates3d(globalLocation);
            var shapeId = gfcDoc.AddGfc2LineShape(locationId, width, leftWidth, lineId, zS, zE);

            var btmElev = archiWall.GlobalZ;
            var topElev = archiWall.GlobalZ + zS;

            var btmElevS = Math.Round(btmElev / 1000, 2).ToString();//单位m，而且这两个是控制实际高度的，不是用真正的几何体。。。。
            var topElevS = Math.Round(topElev / 1000, 2).ToString();

            name = String.Format("内墙{0}", width);

            WallThickness = width;
            IsConstruct = false;
            Model = archiWall;

            ID = THModelToGFC2.ToGfcArchiWall(gfcDoc, globalId, name, shapeId, leftWidth, width, btmElevS, topElevS);

        }


        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCWallModel>().First(o => o.WallThickness == current.WallThickness);
            construct.Primitives.Add(current);
        }
    }
}
