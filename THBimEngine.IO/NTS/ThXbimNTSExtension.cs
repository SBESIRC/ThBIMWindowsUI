using NetTopologySuite.Geometries;
using Xbim.Common.Geometry;

namespace THBimEngine.IO.NTS
{
    public static class ThXbimNTSExtension
    {
        public static XbimPoint3D ToXbimPoint(this Coordinate point)
        {
            return new XbimPoint3D(point.X, point.Y, 0);
        }

        public static XbimPoint3D ToXbimPoint(this Point point)
        {
            return new XbimPoint3D(point.X, point.Y, 0);
        }
    }
}
