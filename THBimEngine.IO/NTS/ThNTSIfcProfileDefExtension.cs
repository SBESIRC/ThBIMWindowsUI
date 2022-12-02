using System.Linq;
using ThBIMServer.NTS;
using Xbim.Ifc2x3.ProfileResource;
using NetTopologySuite.Geometries;
using Xbim.Ifc2x3.GeometricConstraintResource;
using THBimEngine.IO.Xbim;

namespace THBimEngine.IO.NTS
{
    public static class ThNTSIfcProfileDefExtension
    {
        public static Polygon ToPolygon(this IfcProfileDef profile, IfcLocalPlacement localPlacement)
        {
            var lineString = profile.ToXbimFace(localPlacement).OuterBound.Points.ToArray().ToLineString();
            if (lineString is LinearRing ring)
            {
                return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon();
        }
    }
}
