using System;
using System.Collections.Generic;
using System.Linq;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using THBimEngine.IO.Geometry;
using THBimEngine.IO.Xbim;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProfileResource;

namespace ThBIMServer.NTS
{
    public static class ThIFCNTSExtension
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

        public static Polygon ToNTSPolygon(this IfcProduct ifcElement)
        {
            var body = ifcElement.Representation.Representations[0].Items[0];
            if (body is IfcExtrudedAreaSolid extrudedAreaSolid)
            {
                var dir = extrudedAreaSolid.ExtrudedDirection;
                if (dir.X == 0 && dir.Y == 0 && dir.Z == 1)
                {
                    var profile = extrudedAreaSolid.SweptArea;
                    var placement = ifcElement.ObjectPlacement as IfcLocalPlacement;
                    var face = profile.ToXbimFace(placement);
                    return face.ToPolygon();
                }
            }
            return ThXbimGeometryAnalyzer.ElementBottomFace(ifcElement).ToPolygon();
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

        //public static LineString ToNTSLineString(this IfcRectangleProfileDef rectangleProfile, IfcAxis2Placement areaLocation, IfcAxis2Placement placement)
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
            var points = new List<CoordinateZ>();
            if (profile is IfcArbitraryClosedProfileDef closedProfile)
            {
                var curve = closedProfile.OuterCurve;
                if (curve is IfcPolyline polyline)
                {
                    var profOutter = polyline.GetOutterNTS3D();
                    points.AddRange(profOutter);
                }
                else if (curve is IfcCompositeCurve compositeCurve)
                {
                    var profOutter = compositeCurve.GetOutterNTS3D();
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

            placement.ToTransInfo(out var offset, out var offsetV);
            var pointsGlobalTemp = points.Select(x => ThNTSOperation.TransP(x, offsetV, offset)).ToList();

            var pointsGlobal = pointsGlobalTemp.Select(x => new Coordinate(x.X, x.Y)).ToList();
            return pointsGlobal.CreateLineString();
        }

        private static void ToTransInfo(this IfcAxis2Placement placement, out CoordinateZ offset, out List<Vector3D> offsetV)
        {
            offsetV = new List<Vector3D>();

            offset = (placement as IfcPlacement).Location.ToNTSCoordinate3D();
            for (int i = 0; i < placement.P.Count; i++)
            {
                var v = new Vector3D(placement.P[i].X.MakePrecise(), placement.P[i].Y.MakePrecise(), placement.P[i].Z.MakePrecise());
                offsetV.Add(v);
            }
            if (placement.P.Count == 2)
            {
                offsetV.Add(new Vector3D(0, 0, 1));
            }
        }

        /// <summary>
        /// 目前只支持拉伸体且z 0，0，1情况
        /// </summary>
        /// <param name="ifcElement"></param>
        /// <param name="ZValue"></param>
        /// <param name="ZDir"></param>
        public static void GetExtrudedDepth(this IfcProduct ifcElement, out double ZValue, out Vector3D ZDir)
        {
            ZDir = new Vector3D(0, 0, 1);
            ZValue = 0;
            var body = ifcElement.Representation.Representations[0].Items[0];
            if (body is IfcExtrudedAreaSolid extrudedAreaSolid)
            {
                var dir = extrudedAreaSolid.ExtrudedDirection;
                if (dir.X == 0 && dir.Y == 0 && dir.Z == 1)
                {
                    ZDir = new Vector3D(dir.X, dir.Y, dir.Z);
                    ZValue = extrudedAreaSolid.Depth;
                }
            }
        }

        public static void GetGlobleZ(this IfcProduct ifcElement, out double ZHight)
        {
            ZHight = 0;
            var body = ifcElement.Representation.Representations[0].Items[0];
            if (body is IfcExtrudedAreaSolid extrudedAreaSolid)
            {
                var dir = extrudedAreaSolid.ExtrudedDirection;
                if (dir.X == 0 && dir.Y == 0 && dir.Z == 1)
                {
                    var placement = ifcElement.ObjectPlacement as IfcLocalPlacement;
                    ToTransInfo(placement.RelativePlacement, out var offset, out var offsetV);
                    var point = new CoordinateZ(0, 0, 0);
                    var transPt = ThNTSOperation.TransP(point, offsetV, offset);
                    ZHight = transPt.Z;
                }
            }
        }

        //private static List<Coordinate> GetOutterNTS(this IfcRectangleProfileDef rectangleProfile)
        //{
        //    var points = new List<Coordinate>();

        //    var xDim = ((double)rectangleProfile.XDim.Value).MakePrecise();
        //    var yDim = ((double)rectangleProfile.YDim.Value).MakePrecise();

        //    var pointsTemp = new List<Coordinate>();
        //    pointsTemp.Add(new Coordinate(-xDim / 2, -yDim / 2));
        //    pointsTemp.Add(new Coordinate(-xDim / 2, yDim / 2));
        //    pointsTemp.Add(new Coordinate(xDim / 2, yDim / 2));
        //    pointsTemp.Add(new Coordinate(xDim / 2, -yDim / 2));
        //    pointsTemp.Add(new Coordinate(-xDim / 2, -yDim / 2));

        //    ToTransInfo(rectangleProfile.Position, out var offsetLocation, out var offsetV1, out var offsetV2);
        //    points = pointsTemp.Select(x => ThNTSOperation.TransP(x, offsetV1, offsetV2, offsetLocation)).ToList();

        //    return points;
        //}

        private static List<CoordinateZ> GetOutterNTS(this IfcRectangleProfileDef rectangleProfile)
        {
            var points = new List<CoordinateZ>();

            var xDim = ((double)rectangleProfile.XDim.Value).MakePrecise();
            var yDim = ((double)rectangleProfile.YDim.Value).MakePrecise();

            var pointsTemp = new List<CoordinateZ>();
            pointsTemp.Add(new CoordinateZ(-xDim / 2, -yDim / 2, 0));
            pointsTemp.Add(new CoordinateZ(-xDim / 2, yDim / 2, 0));
            pointsTemp.Add(new CoordinateZ(xDim / 2, yDim / 2, 0));
            pointsTemp.Add(new CoordinateZ(xDim / 2, -yDim / 2, 0));
            pointsTemp.Add(new CoordinateZ(-xDim / 2, -yDim / 2, 0));

            ToTransInfo(rectangleProfile.Position, out var offsetLocation, out var offsetV);
            points = pointsTemp.Select(x => ThNTSOperation.TransP(x, offsetV, offsetLocation)).ToList();

            return points;
        }

        /// <summary>
        /// 有bug，底边必须在xy平面，如果在其他平面的拉伸体，则需要coornidateM,暂不支持
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        private static List<Coordinate> GetOutterNTS(this IfcPolyline polyline)
        {
            var points = new List<Coordinate>();

            for (int i = 0; i < polyline.Points.Count; i++)
            {
                points.Add(polyline.Points[i].ToNTSCoordinate3D());
            }
            points.Add(polyline.Points[0].ToNTSCoordinate3D());

            return points;
        }

        /// <summary>
        /// 有bug，底边必须在xy平面，如果在其他平面的拉伸体，则需要coornidateM,暂不支持
        /// </summary>
        /// <param name="curve"></param>
        /// <returns></returns>
        private static List<Coordinate> GetOutterNTS(this IfcCompositeCurve compositeCurve)
        {
            var points = new List<Coordinate>();

            for (int i = 0; i < compositeCurve.Segments.Count; i++)
            {
                points.Add((compositeCurve.Segments[i].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate3D());
            }
            points.Add((compositeCurve.Segments[0].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate3D());

            return points;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static CoordinateZ ToNTSCoordinate3D(this IfcCartesianPoint point)
        {

            if (!point.Z.Equals(double.NaN))
            {
                return new CoordinateZ(point.X.MakePrecise(), point.Y.MakePrecise(), point.Z.MakePrecise());
            }
            else
            {
                return new CoordinateZ(point.X.MakePrecise(), point.Y.MakePrecise(), 0);
            }

        }

        public static Coordinate ToNTSCoordinate(this IfcCartesianPoint point)
        {
            return new Coordinate(point.X.MakePrecise(), point.Y.MakePrecise());
        }



        //////////////////////3d////////////////
        private static List<CoordinateZ> GetOutterNTS3D(this IfcPolyline polyline)
        {
            var points = new List<CoordinateZ>();

            for (int i = 0; i < polyline.Points.Count; i++)
            {
                points.Add(polyline.Points[i].ToNTSCoordinate3D());
            }
            points.Add(polyline.Points[0].ToNTSCoordinate3D());


            return points;
        }

        private static List<CoordinateZ> GetOutterNTS3D(this IfcCompositeCurve compositeCurve)
        {
            var points = new List<CoordinateZ>();

            for (int i = 0; i < compositeCurve.Segments.Count; i++)
            {
                points.Add((compositeCurve.Segments[i].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate3D());
            }
            points.Add((compositeCurve.Segments[0].ParentCurve as IfcPolyline).Points[0].ToNTSCoordinate3D());

            return points;
        }




    }
}
