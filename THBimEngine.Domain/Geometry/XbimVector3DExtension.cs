using System;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain.Geometry
{
    public static class XbimVector3DExtension
    {
        public static double DotProduct(this XbimVector3D v, XbimPoint3D p)
        {
            return (v.X * p.X) + (v.Y * p.Y) + (v.Z * p.Z);
        }

        public static bool IsPerpendicularTo(this XbimVector3D v, XbimVector3D ov, double tolerance = 1e-10)
        {
            return Math.Abs(v.DotProduct(ov)) < tolerance;
        }
    }
}
