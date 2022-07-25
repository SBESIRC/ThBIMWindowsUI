using NetTopologySuite.Geometries;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using THBimEngine.Domain.GeometryModel;
using Xbim.Common.Geometry;

namespace THBimEngine.Geometry.NTS
{
    public static class GeometryDomainExtension
    {
        public static Coordinate ToNTSCoordinate(this Point3DSurrogate point)
        {
            return new Coordinate(point.X,point.Y);
        }
        public static Coordinate ToNTSCoordinate(this XbimPoint3D point)
        {
            return new Coordinate(point.X, point.Y);
        }
        public static LineString ToNTSLineString(this PolylineSurrogate polyline) 
        {
            var points = new List<Coordinate>();
            for (int i = 0; i < polyline.Points.Count; i++)
            {
                var pt1 = polyline.Points[i].Points.First().Point3D2XBimPoint();
                XbimPoint3D? midPt = null;
                if (polyline.Points[i].Points.Count != 1)
                {
                    midPt = polyline.Points[i].Points.Last().Point3D2XBimPoint();
                }
                var pt2 = pt1;
                if (i + 1 < polyline.Points.Count)
                {
                    pt2 = polyline.Points[i + 1].Points.First().Point3D2XBimPoint();
                }
                else
                {
                    pt2 = polyline.Points[0].Points.First().Point3D2XBimPoint();
                }
                if (midPt.HasValue)
                {
                    //圆弧
                }
                else 
                {
                    //线段
                    points.Add(pt1.ToNTSCoordinate());
                }
            }
            if (points.Last().Distance(points.First()) < 1)
            {
                points.Remove(points.Last());
                points.Add(points.First());
            }
            if (polyline.IsClosed) 
            {
                if (points.Last().Distance(points.First()) > 1) 
                {
                    points.Add(points[0]);
                }
            }
            if (points[0].Equals(points[points.Count - 1]))
            {
                // 首尾端点一致的情况
                // LinearRings are the fundamental building block for Polygons.
                // LinearRings may not be degenerate; that is, a LinearRing must have at least 3 points.
                // Other non-degeneracy criteria are implied by the requirement that LinearRings be simple. 
                // For instance, not all the points may be collinear, and the ring may not self - intersect.
                return EngineNTSService.Instance.GeometryFactory.CreateLinearRing(points.ToArray());
            }
            else 
            {
                // 首尾端点不一致的情况
                return EngineNTSService.Instance.GeometryFactory.CreateLineString(points.ToArray());
            } 
        }

        public static Polygon ToNTSPolygon(this PolylineSurrogate polyLine)
        {
            var geometry = polyLine.ToNTSLineString();
            if (geometry is LinearRing ring)
            {
                return EngineNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            else
            {
                return EngineNTSService.Instance.GeometryFactory.CreatePolygon();
            }
        }


        public static Point3DSurrogate ToGePoint3d(this Coordinate point)
        {
            return new Point3DSurrogate
            {
                X = point.X,
                Y = point.Y,
                Z = 0,
            };
        }
        public static PolylineSurrogate ToGePolyline(this LineString lineString)
        {
            var pline = new PolylineSurrogate()
            {
                IsClosed = lineString.IsClosed,
                Points = new List<Point3DCollectionSurrogate>(),
            };
            foreach (var item in lineString.Coordinates) 
            {
                var point = item.ToGePoint3d();
                var coll = new Point3DCollectionSurrogate();
                coll.Points = new List<Point3DSurrogate>();
                coll.Points.Add(point);
                pline.Points.Add(coll);
            }
            return pline;
        }
        public static PolylineSurrogate ToGePolyline(this Polygon polygon)
        {
            var pline = polygon.Shell.ToGePolyline();
            return pline;
        }

        public static PolylineSurrogate ToPolylineSurrogate(this NetTopologySuite.Geometries.Geometry geometry)
        {
            var polyline = new PolylineSurrogate();
            if (geometry.IsEmpty)
            {
                return polyline;
            }
            if (geometry is LineString lineString)
            {
                polyline = lineString.ToGePolyline();
            }
            else if (geometry is LinearRing linearRing)
            {
                polyline = linearRing.ToGePolyline();
            }
            else if (geometry is Polygon polygon)
            {
                polyline = polygon.ToGePolyline();
            }
            else if (geometry is MultiLineString lineStrings)
            {
                
            }
            else if (geometry is MultiPolygon polygons)
            {
                
            }
            else if (geometry is GeometryCollection geometries)
            {
                
            }
            return polyline;
        }
    }
}
