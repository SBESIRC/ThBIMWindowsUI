using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;

using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCCeilingModel : GFCElementModel
    {
        public int CeilingThickness { get; set; }
        public string CeilingName { get; set; }

        public GFCCeilingModel(ThGFC2Document gfcDoc, int globalId, string name, int thickness) : base(gfcDoc, globalId, name)
        {
            CeilingThickness = thickness;
            CeilingName = name;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcCeiling(gfcDoc, globalId, CeilingName, -1, CeilingThickness);
        }

        public GFCCeilingModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel room, int thickness) : base(gfcDoc, globalId, name)
        {
            var location = new XbimMatrix3D(new XbimVector3D(0, 0, room.GlobalZ + room.ZValue));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = room.Outline.Shell.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);

            CeilingThickness = thickness;
            CeilingName = name;
            Model = room;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcCeiling(gfcDoc, globalId, CeilingName, shapeId, CeilingThickness);
        }

        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCCeilingModel>().First(o => o.CeilingName == current.CeilingName);
            construct.Primitives.Add(current);
        }


    }
}
