using System;
using System.Collections.Generic;
using THBimEngine.Domain.Model;
using THBimEngine.Domain.Model.SurrogateModel;
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
        public static XbimPoint3D Point3D2XBimPoint(this Point3DSurrogate point3DSurrogate) 
        {
            return new XbimPoint3D(point3DSurrogate.X, point3DSurrogate.Y, point3DSurrogate.Z);
        }
        public static XbimVector3D Vector3D2XBimVector(this Vector3DSurrogate vector3DSurrogate)
        {
            return new XbimVector3D(vector3DSurrogate.X, vector3DSurrogate.Y, vector3DSurrogate.Z);
        }
        public static XbimVector3D Point3D2Vector(this Point3DSurrogate point3DSurrogate) 
        {
            return new XbimVector3D(point3DSurrogate.X, point3DSurrogate.Y, point3DSurrogate.Z);
        }
        public static XbimVector3D Point3D2Vector(this XbimPoint3D point3D)
        {
            return new XbimVector3D(point3D.X, point3D.Y, point3D.Z);
        }
        public static GeometryParam WallGeometryParam(this ThTCHWall tchWall) 
        {
            if (tchWall.Outline.Points.Count >= 3)
            {
                var outLineGeoParam = new GeometryStretch(tchWall.Outline, tchWall.XVector.Vector3D2XBimVector(),
                                tchWall.ExtrudedDirection.Vector3D2XBimVector(),
                                tchWall.Height);
                return outLineGeoParam;
            }
            else 
            {
                var geoParam = new GeometryStretch(
                                   tchWall.Origin.Point3D2XBimPoint(),
                                   tchWall.XVector.Vector3D2XBimVector(),
                                   tchWall.Length,
                                   tchWall.Width,
                                   tchWall.ExtrudedDirection.Vector3D2XBimVector(),
                                   tchWall.Height);
                return geoParam;
            }
        }
        public static double PointDistanceToPoint(this XbimPoint3D point,XbimPoint3D targetPoint) 
        {
            var disX = (point.X - targetPoint.X);
            var disY = (point.Y - targetPoint.Y);
            var disZ = (point.Z - targetPoint.Z);
            return Math.Sqrt(disX * disX + disY * disY + disZ + disZ);
        }
        public static GeometryParam DoorGeometryParam(this ThTCHDoor tchDoor)
        {
            var geoParam = new GeometryStretch(
                                tchDoor.CenterPoint.Point3D2XBimPoint(),
                                tchDoor.XVector.Point3D2Vector(),
                                tchDoor.Width,
                                tchDoor.Thickness,
                                tchDoor.ExtrudedDirection.Vector3D2XBimVector(),
                                tchDoor.Height);
            return geoParam;
        }
        public static GeometryParam WindowGeometryParam(this ThTCHWindow tchWindow)
        {
            var geoParam = new GeometryStretch(
                                tchWindow.CenterPoint.Point3D2XBimPoint(),
                                tchWindow.XVector.Vector3D2XBimVector(),
                                tchWindow.Width,
                                tchWindow.Thickness,
                                tchWindow.ExtrudedDirection.Vector3D2XBimVector(),
                                tchWindow.Height);
            return geoParam;
        }
        public static GeometryParam OpeningGeometryParam(this ThTCHOpening thcOpening)
        {
            var geoParam = new GeometryStretch(
                                thcOpening.CenterPoint.Point3D2XBimPoint(),
                                thcOpening.XVector.Vector3D2XBimVector(),
                                thcOpening.Width,
                                thcOpening.Thickness,
                                thcOpening.ExtrudedDirection.Vector3D2XBimVector(),
                                thcOpening.Height);
            return geoParam;
        }
    }
}
