using System.Linq;
using System.Collections.Generic;

using NetTopologySuite;
using NetTopologySuite.Geometries;
using System;
using Google.Protobuf;

namespace ThBIMServer.NTS
{
    public static class ThTCHNTSExtension
    {
        private static PrecisionModel PM = NtsGeometryServices.Instance.DefaultPrecisionModel;
        private static GeometryFactory GF = NtsGeometryServices.Instance.CreateGeometryFactory();

        private static Coordinate ToCoordinate(this ThTCHPoint3d point)
        {
            return new Coordinate(PM.MakePrecise(point.X), PM.MakePrecise(point.Y));
        }

        private static ThTCHPoint3d ToTCHPoint(this Coordinate point)
        {
            return new ThTCHPoint3d { X = PM.MakePrecise(point.X), Y = PM.MakePrecise(point.Y), Z = 0 };
        }

        public static LineString ToNTSLineString(this ThTCHLine line)
        {
            var points = new List<Coordinate>();
            points.Add(ToCoordinate(line.StartPt));
            points.Add(ToCoordinate(line.EndPt));
            return GF.CreateLineString(points.ToArray());
        }

        public static LineString ToNTSLineString(this ThTCHArc arc)
        {
            var points = new List<Coordinate>();
            points.Add(ToCoordinate(arc.StartPt));
            points.Add(ToCoordinate(arc.CenterPt));
            points.Add(ToCoordinate(arc.EndPt));
            return GF.CreateLineString(points.ToArray());
        }

        public static LineString ToNTSLineString(this ThTCHPolyline polyline)
        {
            var points = new List<Coordinate>();
            var pts = polyline.Points;
            if (polyline.Segments.Count > 0)
            {
                // 起点
                var startPt = pts[(int)polyline.Segments[0].Index[0]];
                points.Add(ToCoordinate(startPt));
                foreach (var segment in polyline.Segments)
                {
                    if (segment.Index.Count == 2)
                    {
                        //直线段
                        var endPt = pts[(int)segment.Index[1]];
                        points.Add(ToCoordinate(endPt));
                    }
                    else
                    {
                        //圆弧段
                        var midPt = pts[(int)segment.Index[1]];
                        var endPt = pts[(int)segment.Index[2]];
                        points.Add(ToCoordinate(midPt));
                        points.Add(ToCoordinate(endPt));
                    }
                }

                // 支持真实闭合或视觉闭合
                // 对于处于“闭合”状态的多段线，要保证其首尾点一致
                if (points[0].Equals2D(points[points.Count - 1], 1e-8))
                {
                    if (points.Count > 1)
                    {
                        points.RemoveAt(points.Count - 1);
                        points.Add(points[0]);
                    }
                }
                else
                {
                    if (polyline.IsClosed)
                    {
                        points.Add(points[0]);
                    }
                }
            }

            if (points[0].Equals(points[points.Count - 1]))
            {
                // 三个点，其中起点和终点重合
                // 多段线退化成一根线段
                if (points.Count == 3)
                {
                    return GF.CreateLineString(points.ToArray());
                }

                // 二个点，其中起点和终点重合
                // 多段线退化成一个点
                if (points.Count == 2)
                {
                    return GF.CreateLineString();
                }

                // 一个点
                // 多段线退化成一个点
                if (points.Count == 1)
                {
                    return GF.CreateLineString();
                }

                // 首尾端点一致的情况
                // LinearRings are the fundamental building block for Polygons.
                // LinearRings may not be degenerate; that is, a LinearRing must have at least 3 points.
                // Other non-degeneracy criteria are implied by the requirement that LinearRings be simple. 
                // For instance, not all the points may be collinear, and the ring may not self - intersect.
                return GF.CreateLinearRing(points.ToArray());
            }
            else
            {
                // 首尾端点不一致的情况
                return GF.CreateLineString(points.ToArray());
            }
        }

        public static LineString ToNTSLineString(this IMessage entity)
        {
            if(entity is ThTCHLine line)
            {
                return line.ToNTSLineString();
            }
            else if(entity is ThTCHArc arc)
            {
                return arc.ToNTSLineString();
            }
            else if(entity is ThTCHPolyline polyline)
            {
                return polyline.ToNTSLineString();
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public static ThTCHPolyline ToTCHPolyline(this NetTopologySuite.Geometries.Geometry geometry)
        {
            if (geometry.IsEmpty)
            {
                return new ThTCHPolyline();
            }

            if (geometry is LineString lineString)
            {
                return lineString.ToTCHPolyline();
            }
            else if (geometry is LinearRing linearRing)
            {
                return linearRing.ToTCHPolyline();
            }
            else if (geometry is Polygon polygon)
            {
                return polygon.Shell.ToTCHPolyline();
            }
            else
            {
                return new ThTCHPolyline();
            }
        }

        public static ThTCHPolyline ToTCHPolyline(this LineString lineString)
        {
            var tchPolyline = new ThTCHPolyline();
            if (!lineString.IsValid)
            {
                return tchPolyline;
            }
            tchPolyline.IsClosed = lineString.IsClosed;

            tchPolyline.Points.Add(lineString.Coordinates[0].ToTCHPoint());
            uint ptIndex = 0;
            for (int k = 0; k < lineString.Coordinates.Count() - 1; k++)
            {
                if (lineString.Coordinates[k].Distance(lineString.Coordinates[k + 1]) > 10)
                {
                    var tchSegment = new ThTCHSegment();
                    tchSegment.Index.Add(ptIndex);
                    if (k == lineString.Coordinates.Count() - 1 && lineString.IsClosed)
                    {
                        tchSegment.Index.Add(0);
                        tchPolyline.Segments.Add(tchSegment);
                    }
                    else
                    {
                        // 直线段
                        tchPolyline.Points.Add(lineString.Coordinates[k + 1].ToTCHPoint());
                        tchSegment.Index.Add(++ptIndex);
                        tchPolyline.Segments.Add(tchSegment);
                    }
                }
            }
            return tchPolyline;
        }

        public static Polygon ToNTSPolygon(this Google.Protobuf.IBufferMessage gpEntity)
        {
            if(gpEntity is ThTCHMPolygon gpMPolygon)
            {
                return gpMPolygon.ToNTSPolygon();
            }
            else if(gpEntity is ThTCHPolyline gpPolyline)
            {
                return gpPolyline.ToNTSPolygon();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static Polygon ToNTSPolygon(this ThTCHMPolygon polygon)
        {
            if (polygon == null)
            {
                throw new NotImplementedException();
            }
            else if (polygon.Holes.Count == 0)
            {
                var geometry = polygon.Shell.ToNTSLineString();
                return geometry.CreatePolygon();
            }
            else
            {
                if (polygon.Shell.ToNTSLineString() is LinearRing shellRing)
                {
                    var holeRings = polygon.Holes.Select(h => h.ToNTSLineString()).OfType<LinearRing>().ToArray();
                    return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon(shellRing, holeRings);
                }
                else
                {
                    return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon();
                }
            }
        }

        public static Polygon ToNTSPolygon(this ThTCHPolyline polyline)
        {
            if (polyline == null)
            {
                throw new NotImplementedException();
            }
            var geometry = polyline.ToNTSLineString();
            return geometry.CreatePolygon();
        }

        public static ThTCHPoint3d Intersection(this IBufferMessage curve, IBufferMessage other)
        {
            var geometry = curve.ToNTSLineString().Intersection(other.ToNTSLineString());
            if (geometry is Point point)
            {
                return new ThTCHPoint3d()
                {
                    X = point.X,
                    Y = point.Y,
                    Z = 0,
                };
            }
            else
            {
                return null;
            }
        }
    }
}
