using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common.Geometry;

using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCFloorFinishModel : GFCElementModel
    {
        public int FloorFinishThickness { get; set; }
        public string FloorFinishName { get; set; }
        public GFCFloorFinishModel(ThGFC2Document gfcDoc, int globalId, string name, int thickness) : base(gfcDoc, globalId, name)
        {
            FloorFinishThickness = thickness;
            FloorFinishName = name;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcFaceFloorFinish(gfcDoc, globalId, name, -1, thickness, "层底标高");
        }

        public GFCFloorFinishModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel room, int thickness) : base(gfcDoc, globalId, name)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, 0));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);
            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);
            var topElev = Math.Round(room.GlobalZ / 1000, 2).ToString();

            FloorFinishThickness = thickness;
            FloorFinishName = name;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcFaceFloorFinish(gfcDoc, globalId, name, shapeId, thickness, topElev);
        }

        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCFloorFinishModel>().First(o => o.FloorFinishName == current.FloorFinishName);
            construct.Primitives.Add(current);
        }
    }
}
