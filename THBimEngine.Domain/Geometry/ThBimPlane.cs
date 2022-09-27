using System;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain.Geometry
{
    public class ThBimPlane
    {
        public readonly double D;
        public readonly XbimVector3D Normal;

        public ThBimPlane(XbimVector3D normal, XbimPoint3D rootPoint)
        {
            this.Normal = normal;
            this.D = normal.DotProduct(rootPoint);
        }

        public static ThBimPlane FromPoints(XbimPoint3D p1, XbimPoint3D p2, XbimPoint3D p3)
        {
            // http://www.had2know.com/academics/equation-plane-through-3-points.html
            if (p1.Equals(p2) || p1.Equals(p3) || p2.Equals(p3))
            {
                throw new ArgumentException("Must use three different points");
            }

            var v1 = new XbimVector3D(p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            var v2 = new XbimVector3D(p3.X - p1.X, p3.Y - p1.Y, p3.Z - p1.Z);
            var cross = v1.CrossProduct(v2);

            if (cross.Length <= float.Epsilon)
            {
                throw new ArgumentException("The 3 points should not be on the same line");
            }

            return new ThBimPlane(cross.Normalized(), p1);
        }

        public XbimPoint3D Project(XbimPoint3D p, XbimVector3D? projectionDirection = null)
        {
            var dotProduct = this.Normal.DotProduct(p.ToVector3D());
            var projectiononNormal = projectionDirection == null ? this.Normal : projectionDirection.Value;
            var projectionVector = (dotProduct + this.D) * projectiononNormal;
            return p - projectionVector;
        }

        public double SignedDistanceTo(XbimPoint3D point)
        {
            var p = this.Project(point);
            var v = p.VectorTo(point);
            return v.DotProduct(this.Normal);
        }

        public XbimPoint3D IntersectionWith(THBimRay3D ray, double tolerance = float.Epsilon)
        {
            if (this.Normal.IsPerpendicularTo(ray.Direction, tolerance))
            {
                throw new InvalidOperationException("Ray is parallel to the plane.");
            }

            var d = this.SignedDistanceTo(ray.ThroughPoint);
            var t = -1 * d / ray.Direction.DotProduct(this.Normal);
            return ray.ThroughPoint + (t * ray.Direction);
        }
    }
}
