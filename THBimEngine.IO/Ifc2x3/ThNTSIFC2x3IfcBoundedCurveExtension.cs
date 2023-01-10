using System;
using System.Linq;
using NetTopologySuite.Geometries;
using Xbim.Ifc2x3.GeometryResource;

namespace THBimEngine.IO.Ifc2x3
{
    public static class ThNTSIFC2x3IfcBoundedCurveExtension
    {
        public static LineString ToLineString(this IfcPolyline polyline)
        {
            var coordinates = polyline.Points.Select(o => o.ToNTSCoordinate()).ToArray();
            if (coordinates.First() == coordinates.Last())
            {
                return ThBimNTSService.Instance.GeometryFactory.CreateLinearRing(coordinates);
            }
            return ThBimNTSService.Instance.GeometryFactory.CreateLineString(coordinates);
        }

        public static Polygon ToPolygon(this IfcPolyline polyline)
        {
            var lineString = polyline.ToLineString();
            if (lineString is LinearRing ring)
            {
                return ThBimNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            return ThBimNTSService.Instance.GeometryFactory.CreatePolygon();
        }

        public static LineString ToLineString(this IfcCompositeCurve compositeCurve)
        {
            throw new NotImplementedException();
        }

        public static Polygon ToPolygon(this IfcCompositeCurve compositeCurve)
        {
            var lineString = compositeCurve.ToLineString();
            if (lineString is LinearRing ring)
            {
                return ThBimNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            return ThBimNTSService.Instance.GeometryFactory.CreatePolygon();
        }
    }
}
