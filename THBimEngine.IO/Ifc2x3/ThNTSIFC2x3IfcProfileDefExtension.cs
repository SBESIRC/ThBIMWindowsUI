using System;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometryResource;
using NetTopologySuite.Geometries;

namespace THBimEngine.IO.Ifc2x3
{
    public static class ThNTSIFC2x3IfcProfileDefExtension
    {
        public static Polygon ToPolygon(this IfcArbitraryClosedProfileDef profileDef)
        {
            if (profileDef.OuterCurve is IfcPolyline polyline)
            {
                return polyline.ToPolygon();
            }
            else if (profileDef.OuterCurve is IfcCompositeCurve curve)
            {
                return curve.ToPolygon();
            }
            throw new NotSupportedException();
        }
    }
}
