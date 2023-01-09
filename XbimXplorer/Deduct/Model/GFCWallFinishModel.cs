using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common.Geometry;
using NetTopologySuite.Geometries;

using THBimEngine.IO.GFC2;
using THBimEngine.Domain;

namespace XbimXplorer.Deduct.Model
{
    public class GFCWallFinishModel : GFCElementModel
    {
        public int WallFinishThickness { get; set; }
        public string WallFinishName { get; set; }
        public GFCWallFinishModel(ThGFC2Document gfcDoc, int globalId, string name, int thickness) : base(gfcDoc, globalId, name)
        {
            WallFinishName = name;
            WallFinishThickness = thickness;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcWallFaceFinish(gfcDoc, globalId, name, -1, thickness, "墙底标高", "墙顶标高", 50);
        }


        /// <summary>
        /// 这里有一个很crazy的地方，axisOffset是距墙的左面距离值，但是当axisOffset值为0时，它会布置在墙的右面。
        /// </summary>
        /// <param name="gfcDoc"></param>
        /// <param name="name"></param>
        /// <param name="globalId"></param>
        /// <param name="wall"></param>
        /// <param name="thickness"></param>
        /// <param name="wallFaceFinishLine"></param>
        public GFCWallFinishModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel wall, int thickness, LineString wallFaceFinishLine) : base(gfcDoc, globalId, name)
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
            var axisOffset = isLeft ? 50 : 0;

            WallFinishName = name;
            WallFinishThickness = thickness;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcWallFaceFinish(gfcDoc, globalId, name, shapeId, thickness, btmElevS, topElevS, axisOffset);
        }

        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCWallFinishModel>().First(o => o.WallFinishName == current.WallFinishName);
            construct.Primitives.Add(current);
        }

        private static Tuple<XbimPoint3D, XbimPoint3D> ExtendLine(LineString line, double distance)
        {
            var stPt = new XbimPoint3D(line.StartPoint.X, line.StartPoint.Y, 0);
            var endPt = new XbimPoint3D(line.EndPoint.X, line.EndPoint.Y, 0);
            var direction = (endPt - stPt).Normalized();
            var newSPt = stPt - direction * distance;
            var newEPt = endPt + direction * distance;
            return (new XbimPoint3D(Math.Round(newSPt.X, 2), Math.Round(newSPt.Y, 2), 0), new XbimPoint3D(Math.Round(newEPt.X, 2), Math.Round(newEPt.Y, 2), 0)).ToTuple();
        }

    }
}
