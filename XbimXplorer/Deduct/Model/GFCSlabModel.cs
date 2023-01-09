using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common.Geometry;

using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCSlabModel : GFCElementModel
    {
        public int SlabThickness { get; set; }
        public GFCSlabModel(ThGFC2Document gfcDoc, int globalId, string name, int thickness) : base(gfcDoc, globalId, name)
        {
            SlabThickness = thickness;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcSlab(gfcDoc, globalId, name, -1, thickness);
        }

        public GFCSlabModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel slab) : base(gfcDoc, globalId, name)
        {
            var thickness = (int)Math.Round(slab.ZValue);

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, slab.GlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location);

            var pts = slab.Outline.Coordinates.ToList();
            var polyId = gfcDoc.AddSimpolyPolygon(pts);
            var shapeId = gfcDoc.AddFaceShape(localCoordinateId, thickness, polyId);

            SlabThickness = thickness;
            Model = slab;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcSlab(gfcDoc, globalId, name, shapeId, thickness);
        }

        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCSlabModel>().First(o => o.SlabThickness == current.SlabThickness);
            construct.Primitives.Add(current);
        }
    }
}
