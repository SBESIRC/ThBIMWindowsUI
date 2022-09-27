using System;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain.Geometry
{
    public class ThBimCircle3D
    {
        public readonly double Radius;
        public readonly XbimVector3D Axis;
        public readonly XbimPoint3D CenterPoint;

        public ThBimCircle3D(XbimPoint3D centerPoint, XbimVector3D axis, double radius)
        {
            this.CenterPoint = centerPoint;
            this.Axis = axis;
            this.Radius = radius;
        }

        public static ThBimCircle3D FromPoints(XbimPoint3D p1, XbimPoint3D p2, XbimPoint3D p3)
        {
            // https://www.physicsforums.com/threads/equation-of-a-circle-through-3-points-in-3d-space.173847/
            //// ReSharper disable InconsistentNaming
            var p1p2 = p2 - p1;
            var p2p3 = p3 - p2;
            //// ReSharper restore InconsistentNaming

            var axis = XbimVector3D.CrossProduct(p1p2, p2p3).Normalized();
            var midPointA = p1 + (0.5 * p1p2);
            var midPointB = p2 + (0.5 * p2p3);

            var directionA = p1p2.CrossProduct(axis);
            var directionB = p2p3.CrossProduct(axis);

            var bisectorA = new THBimRay3D(midPointA, directionA);
            var bisectorB = ThBimPlane.FromPoints(midPointB, midPointB + directionB.Normalized(), midPointB + axis);

            var center = bisectorA.IntersectionWith(bisectorB);
            if (center == null)
            {
                throw new ArgumentException("A circle cannot be created from these points, are they collinear?");
            }

            return new ThBimCircle3D(center.Value, axis, center.Value.DistanceTo(p1));
        }
    }
}
