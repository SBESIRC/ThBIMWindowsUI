using NetTopologySuite.Geometries;
using Xbim.Ifc2x3.GeometryResource;

namespace THBimEngine.IO.Ifc2x3
{
    public static class ThNTSIFC2x3Extension
    {
        public static double MakePrecise(this double value)
        {
            return ThBimNTSService.Instance.PrecisionModel.MakePrecise(value);
        }

        public static Coordinate ToNTSCoordinate(this IfcCartesianPoint point)
        {

            return new Coordinate(point.X.MakePrecise(), point.Y.MakePrecise());
        }
    }
}
