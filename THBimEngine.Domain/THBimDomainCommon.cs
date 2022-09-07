using System;
using System.Collections.Generic;
using THBimEngine.Domain.GeometryModel;
using THBimEngine.Domain.Model;
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
        public static XbimPoint3D TransPoint(this XbimPoint3D xbimPoint, XbimMatrix3D xbimMatrix)
        {
            return xbimMatrix.Transform(xbimPoint);
        }
        public static XbimMatrix3D ToXBimMatrix3D(this Matrix3DSurrogate matrix3DSurrogate)
        {
            if (matrix3DSurrogate.Data == null || matrix3DSurrogate.Data.Length < 1)
                return WordMatrix;
            //4x4
            var size = matrix3DSurrogate.Data.Length;
            if (Math.Sqrt(size) == 4)
            {

            }
            return WordMatrix;
        }
        public static double PointDistanceToPoint(this XbimPoint3D point, XbimPoint3D targetPoint)
        {
            var disX = (point.X - targetPoint.X);
            var disY = (point.Y - targetPoint.Y);
            var disZ = (point.Z - targetPoint.Z);
            return Math.Sqrt(disX * disX + disY * disY + disZ + disZ);
        }

        public static GeometryParam SlabGeometryParam(this ThTCHSlab tchElement, out List<GeometryStretch> slabDescendingData)
        {
            slabDescendingData = new List<GeometryStretch>();
            var outLineGeoParam = new GeometryStretch(tchElement.Outline, XAxis, ZAxis.Negated(), tchElement.Height);
            if (null != tchElement.Descendings)
            {
                foreach (var item in tchElement.Descendings)
                {
                    if (item.IsDescending)
                    {
                        //降板
                        var desGeoStretch = new GeometryStretch(item.Outline, XAxis, ZAxis.Negated(), item.DescendingThickness, item.DescendingHeight);
                        desGeoStretch.YAxisLength = item.DescendingWrapThickness;
                        desGeoStretch.OutlineBuffer = item.OutlineBuffer;
                        slabDescendingData.Add(desGeoStretch);
                    }
                    else
                    {
                        //洞口
                        outLineGeoParam.Outline.InnerPolylines.Add(item.Outline);
                        var outline = new PolylineSurrogate
                        {
                            Points = outLineGeoParam.Outline.Points,
                            IsClosed = outLineGeoParam.Outline.IsClosed,
                            HolesMaxHeight = item.DescendingHeight,
                            InnerPolylines = outLineGeoParam.Outline.InnerPolylines,
                        };
                        outLineGeoParam.Outline = outline;
                    }
                }
            }
            return outLineGeoParam;
        }

        public static GeometryParam THTCHGeometryParam(this ThTCHElement tchElement)
        {
            var xVector = tchElement.XVector.Vector3D2XBimVector();
            if (tchElement.Outline.Points != null && tchElement.Outline.Points.Count >= 2)
            {
                var outLineGeoParam = new GeometryStretch(
                                        tchElement.Outline,
                                        xVector,
                                        tchElement.ExtrudedDirection.Vector3D2XBimVector(),
                                        tchElement.Height,
                                        tchElement.ZOffSet);
                outLineGeoParam.YAxisLength = tchElement.Width;
                return outLineGeoParam;
            }
            else
            {
                var geoParam = new GeometryStretch(
                                   tchElement.Origin.Point3D2XBimPoint(),
                                   xVector,
                                   tchElement.Length,
                                   tchElement.Width,
                                   tchElement.ExtrudedDirection.Vector3D2XBimVector(),
                                   tchElement.Height,
                                   tchElement.ZOffSet);
                return geoParam;
            }
        }
    }
}
