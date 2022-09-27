using Xbim.Common.Geometry;

namespace THBimEngine.Domain.Geometry
{
    public static class XbimPoint3DExtension
    {
        public static XbimVector3D ToVector3D(this XbimPoint3D p)
        {
            return new XbimVector3D(p.X, p.Y, p.Z);
        }

        public static XbimVector3D VectorTo(this XbimPoint3D p, XbimPoint3D op)
        {
            return op - p;
        }

        public static double DistanceTo(this XbimPoint3D p, XbimPoint3D op)
        {
            var vector = p.VectorTo(op);
            return vector.Length;
        }
    }
}