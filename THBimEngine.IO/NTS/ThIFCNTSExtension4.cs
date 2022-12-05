using System;
using System.Collections.Generic;
using System.Linq;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.ProfileResource;

namespace ThBIMServer.NTS
{
    /// <summary>
    /// 20221205update： ifc4很久没更新，有问题。如果需要用从ThIFCNTSExtension copy一份过来
    /// </summary>
    public static class ThIFCNTSExtension4
    {
        public static Geometry ToNTSGeometry(this IfcProfileDef profile, IfcAxis2Placement placement)
        {
            if (profile is IfcArbitraryClosedProfileDef closedProfile)
            {
                return closedProfile.ToNTSPolygon(placement);
            }
            else if (profile is IfcRectangleProfileDef recProfile)
            {
                return recProfile.ToNTSPolygon(placement);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public static Polygon ToNTSPolygon(this IfcProfileDef profile, IfcAxis2Placement placement)
        {
            if (profile is IfcArbitraryClosedProfileDef closedProfile)
            {
                var geometry = profile.ToNTSLineString(placement);
                return geometry.CreatePolygon();
            }
            else if (profile is IfcRectangleProfileDef rectangleProfile)
            {
                var geometry = profile.ToNTSLineString(placement);
                return geometry.CreatePolygon();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        //public static Polygon ToNTSPolygon(this IfcCurve curve, IfcAxis2Placement placement)
        //{
        //    //if (curve.Area() < 1e-6)
        //    //{
        //    //    return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon();
        //    //}
        //    var geometry = curve.ToNTSLineString(placement);
        //    return geometry.CreatePolygon();
        //}

        //public static Polygon ToNTSPolygon(this IfcRectangleProfileDef rectangleProfile, IfcAxis2Placement placement)
        //{
        //    //if (curve.Area() < 1e-6)
        //    //{
        //    //    return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon();
        //    //}
        //    var geometry = rectangleProfile.ToNTSLineString(placement);
        //    return geometry.CreatePolygon();
        //}


        //public static LineString ToNTSLineString(this IfcCurve curve, IfcAxis2Placement placement)
        //{
        //    if (curve is IfcPolyline polyline)
        //    {
        //        return polyline.ToNTSLineString(placement);
        //    }
        //    else if (curve is IfcCompositeCurve compositeCurve)
        //    {
        //        return compositeCurve.ToNTSLineString(placement);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }
        //}


        //public static LineString ToNTSLineString(this IfcPolyline polyline, IfcAxis2Placement placement)
        //{
        //    var points = new List<Coordinate>();
        //    var offset = (placement as IfcPlacement).Location.ToNTSCoordinate();
        //    for (int i = 0; i < polyline.Points.Count; i++)
        //    {
        //        points.Add(polyline.Points[i].ToNTSCoordinate().Offset(offset));
        //    }
        //    points.Add(polyline.Points[0].ToNTSCoordinate().Offset(offset));

        //    return points.CreateLineString();
        //}

        //public static LineString ToNTSLineString(this IfcRectangleProfileDef rectangleProfile, IfcAxis2Placement placement)
        //{
        //    var points = new List<Coordinate>();
        //    var location = new Coordinate(rectangleProfile.Position.Location.X.MakePrecise(), rectangleProfile.Position.Location.Y.MakePrecise());
        //    var vector1 = new Vector2D(rectangleProfile.Position.P[0].X.MakePrecise(), rectangleProfile.Position.P[0].Y.MakePrecise());
        //    var vector2 = new Vector2D(rectangleProfile.Position.P[1].X.MakePrecise(), rectangleProfile.Position.P[1].Y.MakePrecise());
        //    var xDim = ((double)rectangleProfile.XDim.Value).MakePrecise();
        //    var yDim = ((double)rectangleProfile.YDim.Value).MakePrecise();


        //    var offset = (placement as IfcPlacement).Location.ToNTSCoordinate();
        //    var offsetV1 = new Vector2D(placement.P[0].X.MakePrecise(), placement.P[0].Y.MakePrecise());
        //    var offsetV2 = new Vector2D(placement.P[1].X.MakePrecise(), placement.P[1].Y.MakePrecise());
        //    //offset = new Coordinate(offset.X * offsetV1.X + offset.X * offsetV2.X, offset.Y * offsetV1.Y  + offset.Y * offsetV2.Y);

        //    //var a = areaLocation as IfcAxis2Placement3D;

        //    //var bodyLocation = new Coordinate(a.Location.X.MakePrecise(), a.Location.Y.MakePrecise());
        //    //var bodyv1 = new Vector2D(a.P[0].X.MakePrecise(), rectangleProfile.Position.P[0].Y.MakePrecise());
        //    //var bodyv2 = new Vector2D(a.P[1].X.MakePrecise(), rectangleProfile.Position.P[1].Y.MakePrecise());

        //    var p1 = ThNTSOperation.Addition(location, vector1, vector2, xDim / 2, yDim / 2);
        //    var p2 = ThNTSOperation.Addition(location, -vector1, vector2, xDim / 2, yDim / 2);
        //    var p3 = ThNTSOperation.Addition(location, -vector1, -vector2, xDim / 2, yDim / 2);
        //    var p4 = ThNTSOperation.Addition(location, vector1, -vector2, xDim / 2, yDim / 2);
        //    var p5 = ThNTSOperation.Addition(location, vector1, vector2, xDim / 2, yDim / 2);

        //    var p1s = new Coordinate(p1.X * offsetV1.X + p1.Y * offsetV1.Y, p1.X * offsetV2.X + p1.Y * offsetV2.Y);
        //    var p2s = new Coordinate(p2.X * offsetV1.X + p2.Y * offsetV1.Y, p2.X * offsetV2.X + p2.Y * offsetV2.Y);
        //    var p3s = new Coordinate(p3.X * offsetV1.X + p3.Y * offsetV1.Y, p3.X * offsetV2.X + p3.Y * offsetV2.Y);
        //    var p4s = new Coordinate(p4.X * offsetV1.X + p4.Y * offsetV1.Y, p4.X * offsetV2.X + p4.Y * offsetV2.Y);
        //    var p5s = new Coordinate(p5.X * offsetV1.X + p5.Y * offsetV1.Y, p5.X * offsetV2.X + p5.Y * offsetV2.Y);

        //    points.Add(p1s.Offset(offset));
        //    points.Add(p2s.Offset(offset));
        //    points.Add(p3s.Offset(offset));
        //    points.Add(p4s.Offset(offset));
        //    points.Add(p5s.Offset(offset));

        //    return points.CreateLineString();

        //}

        //public static LineString ToNTSLineString(this IfcCompositeCurve compositeCurve, IfcAxis2Placement placement)
        //{
        //    var points = new List<Coordinate>();
        //    var offset = (placement as IfcPlacement).Location.ToNTSCoordinate();
        //    for (int i = 0; i < compositeCurve.Segments.Count; i++)
        //    {
        //        points.Add((compositeCurve.Segments[i].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate().Offset(offset));
        //    }
        //    points.Add((compositeCurve.Segments[0].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate().Offset(offset));

        //    return points.CreateLineString();
        //}

        //public static LineString ToNTSLineString(this IfcRectangleProfileDef rectangleProfile, IfcAxis2Placement placement)
        //{
        //    var points = new List<Coordinate>();
        //    var location = new Coordinate(rectangleProfile.Position.Location.X.MakePrecise(), rectangleProfile.Position.Location.Y.MakePrecise());
        //    var vector1 = new Vector2D(rectangleProfile.Position.P[0].X.MakePrecise(), rectangleProfile.Position.P[0].Y.MakePrecise());
        //    var vector2 = new Vector2D(rectangleProfile.Position.P[1].X.MakePrecise(), rectangleProfile.Position.P[1].Y.MakePrecise());
        //    var xDim = ((double)rectangleProfile.XDim.Value).MakePrecise();
        //    var yDim = ((double)rectangleProfile.YDim.Value).MakePrecise();
        //    var offset = (placement as IfcPlacement).Location.ToNTSCoordinate();

        //    points.Add(ThNTSOperation.Addition(location, vector1, vector2, xDim, yDim).Offset(offset));
        //    points.Add(ThNTSOperation.Addition(location, -vector1, vector2, xDim, yDim).Offset(offset));
        //    points.Add(ThNTSOperation.Addition(location, -vector1, -vector2, xDim, yDim).Offset(offset));
        //    points.Add(ThNTSOperation.Addition(location, vector1, -vector2, xDim, yDim).Offset(offset));
        //    points.Add(ThNTSOperation.Addition(location, vector1, vector2, xDim, yDim).Offset(offset));

        //    return points.CreateLineString();
        //}

        public static LineString ToNTSLineString(this IfcProfileDef profile, IfcAxis2Placement placement)
        {
            var points = new List<Coordinate>();
            if (profile is IfcArbitraryClosedProfileDef closedProfile)
            {
                var curve = closedProfile.OuterCurve;
                if (curve is IfcPolyline polyline)
                {
                    var profOutter = polyline.GetOutterNTS();
                    points.AddRange(profOutter);
                }
                else if (curve is IfcCompositeCurve compositeCurve)
                {
                    var profOutter = compositeCurve.GetOutterNTS();
                    points.AddRange(profOutter);
                }

            }
            else if (profile is IfcRectangleProfileDef rectangleProfile)
            {
                var profOutter = rectangleProfile.GetOutterNTS();
                points.AddRange(profOutter);
            }
            else
            {
                throw new NotImplementedException();
            }

            placement.ToTransInfo(out var offset, out var offsetV1, out var offsetV2);
            var pointsGlobal = points.Select(x => ThNTSOperation.TransP(x, offsetV1, offsetV2, offset)).ToList();
            return pointsGlobal.CreateLineString();
        }

        private static void ToTransInfo(this IfcAxis2Placement placement, out Coordinate offset, out Vector2D offsetV1, out Vector2D offsetV2)
        {
            offset = (placement as IfcPlacement).Location.ToNTSCoordinate();
            offsetV1 = new Vector2D(placement.P[0].X.MakePrecise(), placement.P[0].Y.MakePrecise());
            offsetV2 = new Vector2D(placement.P[1].X.MakePrecise(), placement.P[1].Y.MakePrecise());
        }

        private static List<Coordinate> GetOutterNTS(this IfcRectangleProfileDef rectangleProfile)
        {
            var points = new List<Coordinate>();

            var xDim = ((double)rectangleProfile.XDim.Value).MakePrecise();
            var yDim = ((double)rectangleProfile.YDim.Value).MakePrecise();

            var pointsTemp = new List<Coordinate>();
            pointsTemp.Add(new Coordinate(-xDim / 2, -yDim / 2));
            pointsTemp.Add(new Coordinate(-xDim / 2, yDim / 2));
            pointsTemp.Add(new Coordinate(xDim / 2, yDim / 2));
            pointsTemp.Add(new Coordinate(xDim / 2, -yDim / 2));
            pointsTemp.Add(new Coordinate(-xDim / 2, -yDim / 2));

            ToTransInfo(rectangleProfile.Position, out var offsetLocation, out var offsetV1, out var offsetV2);
            points = pointsTemp.Select(x => ThNTSOperation.TransP(x, offsetV1, offsetV2, offsetLocation)).ToList();

            return points;
        }

        private static List<Coordinate> GetOutterNTS(this IfcPolyline polyline)
        {
            var points = new List<Coordinate>();

            for (int i = 0; i < polyline.Points.Count; i++)
            {
                points.Add(polyline.Points[i].ToNTSCoordinate());
            }
            points.Add(polyline.Points[0].ToNTSCoordinate());


            return points;
        }

        /// <summary>
        /// 可能有bug
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        private static List<Coordinate> GetOutterNTS(this IfcCompositeCurve compositeCurve)
        {
            var points = new List<Coordinate>();

            for (int i = 0; i < compositeCurve.Segments.Count; i++)
            {
                points.Add((compositeCurve.Segments[i].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate());
            }
            points.Add((compositeCurve.Segments[0].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate());

            return points;
        }

        public static Coordinate ToNTSCoordinate(this IfcCartesianPoint point)
        {
            return new Coordinate(point.X.MakePrecise(), point.Y.MakePrecise());
        }


    }
}
