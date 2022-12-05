
using System;
using System.Collections.Generic;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

namespace ThBIMServer.NTS
{
    public static class ThNTSOperation
    {
        /// <summary>
        /// ?>???????
        /// </summary>
        /// <param name="location"></param>
        /// <param name="vector1"></param>
        /// <param name="vector2"></param>
        /// <param name="xDim"></param>
        /// <param name="yDim"></param>
        /// <returns></returns>
        public static Coordinate Addition(Coordinate location, Vector2D vector1, Vector2D vector2, double xDim, double yDim)
        {
            return new Coordinate(location.X + vector1.X * xDim + vector2.X * yDim, location.Y + vector1.Y * xDim + vector2.Y * yDim);
        }

        public static Coordinate TransP(Coordinate p, Vector2D offsetV1, Vector2D offsetV2, Coordinate offsetPosition)
        {
            var x1 = p.X * offsetV1.X + p.Y * offsetV2.X;
            var x2 = p.X * offsetV1.Y + p.Y * offsetV2.Y;
            //var p2 = new Coordinate(p.X * offsetV1.X + p.Y * offsetV2.X, p.Y * offsetV2.X + p.Y * offsetV2.Y);
            var p2 = new Coordinate(x1, x2);
            var p3 = p2.Offset(offsetPosition);
            return p3;
        }

        public static CoordinateZ TransP(CoordinateZ p, List<Vector3D> offsetV, CoordinateZ offsetPosition)
        {
            var x1 = p.X * offsetV[0].X + p.Y * offsetV[1].X + p.Z * offsetV[2].X;
            var x2 = p.X * offsetV[0].Y + p.Y * offsetV[1].Y + p.Z * offsetV[2].Y;
            var x3 = p.X * offsetV[0].Z + p.Y * offsetV[1].Z + p.Z * offsetV[2].Z;


            //var p2 = new Coordinate(p.X * offsetV1.X + p.Y * offsetV2.X, p.Y * offsetV2.X + p.Y * offsetV2.Y);
            var p2 = new CoordinateZ(x1, x2, x3);

            var p3 = p2.Offset(offsetPosition);
            return p3;
        }

        public static Polygon CreatePolygon(this LineString geometry)
        {
            if (geometry is LinearRing ring)
            {
                return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            else
            {
                return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon();
            }
        }

        public static double MakePrecise(this double value)
        {
            return ThIFCNTSService.Instance.PrecisionModel.MakePrecise(value);
        }

        public static Coordinate Offset(this Coordinate first, Coordinate second)
        {
            return new Coordinate(first.X + second.X, first.Y + second.Y);
        }
        public static CoordinateZ Offset(this CoordinateZ first, CoordinateZ second)
        {
            return new CoordinateZ(first.X + second.X, first.Y + second.Y, first.Z + second.Z);
        }

        public static LineString CreateLineString(this List<Coordinate> points)
        {
            if (points.Count == 0)
            {
                throw new NotSupportedException();
            }

            // 支持真实闭合或视觉闭合
            // 对于处于“闭合”状态的多段线，要保证其首尾点一致
            if (points[0].Equals2D(points[points.Count - 1], ThIFCNTSService.Instance.AcadGlobalTolerance))
            {
                if (points.Count > 1)
                {
                    points.RemoveAt(points.Count - 1);
                    points.Add(points[0]);
                }
            }
            else
            {
                //if (polyline.Closed)
                //{
                //    points.Add(points[0]);
                //}
            }

            if (points[0].Equals(points[points.Count - 1]))
            {
                // 三个点，其中起点和终点重合
                // 多段线退化成一根线段
                if (points.Count == 3)
                {
                    return ThIFCNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
                }

                // 二个点，其中起点和终点重合
                // 多段线退化成一个点
                if (points.Count == 2)
                {
                    return ThIFCNTSService.Instance.GeometryFactory.CreateLineString();
                }

                // 一个点
                // 多段线退化成一个点
                if (points.Count == 1)
                {
                    return ThIFCNTSService.Instance.GeometryFactory.CreateLineString();
                }

                // 首尾端点一致的情况
                // LinearRings are the fundamental building block for Polygons.
                // LinearRings may not be degenerate; that is, a LinearRing must have at least 3 points.
                // Other non-degeneracy criteria are implied by the requirement that LinearRings be simple. 
                // For instance, not all the points may be collinear, and the ring may not self - intersect.
                return ThIFCNTSService.Instance.GeometryFactory.CreateLinearRing(points.ToArray());
            }
            else
            {
                // 首尾端点不一致的情况
                return ThIFCNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
            }
        }

    }
}
