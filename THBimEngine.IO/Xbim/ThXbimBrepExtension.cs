using System.Linq;
using Xbim.Common.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite;

namespace THBimEngine.IO.Xbim
{
    public static class ThXbimBrepExtension
    {
        private static readonly GeometryFactory GF = NtsGeometryServices.Instance.CreateGeometryFactory();

        public static Polygon ToPolygon(this IXbimFace face)
        {
            return GF.CreatePolygon(face.OuterBound.Points.ToLineString() as LinearRing,
                face.InnerBounds.Select(o => o.Points.ToLineString() as LinearRing).ToArray());
        }
    }
}
