using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public static class THBimDomainCommon
    {
        public static readonly double DistTolerance = 1.0;
        public static readonly double AngleTolerance = 0.02;

        public static bool FloatEquals(this double float1, double float2)
        {
            return Math.Abs(float1 - float2) < DistTolerance;
        }

        public static readonly XbimVector3D XAxis = new XbimVector3D(1, 0, 0);
        public static readonly XbimVector3D YAxis = new XbimVector3D(0, 1, 0);
        public static readonly XbimVector3D ZAxis = new XbimVector3D(0, 0, 1);
        public static readonly XbimMatrix3D WordMatrix = new XbimMatrix3D(XbimVector3D.Zero);
        public static XbimVector3D Point3D2Vector(this XbimPoint3D point3D)
        {
            return new XbimVector3D(point3D.X, point3D.Y, point3D.Z);
        }
        public static XbimPoint3D TransPoint(this XbimPoint3D xbimPoint, XbimMatrix3D xbimMatrix)
        {
            return xbimMatrix.Transform(xbimPoint);
        }
        public static bool IsVertical(this XbimVector3D v1, XbimVector3D v2, double angularTolerance)
        {
            var angle = v1.Angle(v2);
            return Math.Abs(angle - Math.PI * 0.5) < angularTolerance;
        }
        public static double PointDistanceToPoint(this XbimPoint3D point, XbimPoint3D targetPoint)
        {
            var disX = (point.X - targetPoint.X);
            var disY = (point.Y - targetPoint.Y);
            var disZ = (point.Z - targetPoint.Z);
            return Math.Sqrt(disX * disX + disY * disY + disZ * disZ);
        }
        public static XbimPoint3D GetCenter(this XbimPoint3D point1, XbimPoint3D point2)
        {
            return new XbimPoint3D((point1.X + point2.X) / 2, (point1.Y + point2.Y) / 2, (point1.Z + point2.Z) / 2);
        }

        public static XbimPoint3D GetPlaneCenter(this IEnumerable<XbimPoint3D> points)
        {
            var max_X = points.Max(o => o.X);
            var max_Y = points.Max(o => o.Y);
            var min_X = points.Min(o => o.X);
            var min_Y = points.Min(o => o.Y);
            return new XbimPoint3D((max_X + min_X) / 2, (max_Y + min_Y) / 2, points.First().Z);
        }

        public static bool IsLeftPt(this XbimPoint3D pt,XbimPoint3D pt1, XbimPoint3D pt2)
        {
            XbimVector3D vector = pt2 - pt1;
            var leftvector = ZAxis.CrossProduct(vector);
            var ptVector = pt - pt1;
            return ptVector.Angle(leftvector) < Math.PI / 2;
        }
    }
}
