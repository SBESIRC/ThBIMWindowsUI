using System.Linq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Xbim.Common.Geometry;

namespace THBimEngine.IO
{
    public static class ThBimXbimNTSMarshal
    {
        private static readonly PrecisionModel PM = NtsGeometryServices.Instance.DefaultPrecisionModel;
        private static readonly GeometryFactory GF = NtsGeometryServices.Instance.CreateGeometryFactory();

        public static Coordinate ToCoordinate(this XbimPoint3D xbimPoint)
        {
            return new Coordinate(PM.MakePrecise(xbimPoint.X), PM.MakePrecise(xbimPoint.Z));
        }

        public static LineString ToLineString(this XbimPoint3D[] xbimPoints)
        {
            return GF.CreateLineString(xbimPoints.Select(o => o.ToCoordinate()).ToArray());
        }
    }
}
