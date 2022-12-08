using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using ThBIMServer.NTS;

namespace THBimEngine.IO.GoogleProtobuf
{
    public enum BufferType
    {
        None,
        BUTT,
        MITTER,
        //SQUARE_OFF,
    }

    public class GPEntityConvert
    {
        BufferType _wallBufferType;
        Dictionary<WallGeometry, ThTCHWallData> wallDataDic = new Dictionary<WallGeometry, ThTCHWallData>();
        List<IBufferMessage> addDBColl = new List<IBufferMessage>();
        List<IBufferMessage> wallCurves = new List<IBufferMessage>();

        public GPEntityConvert()
        {
            _wallBufferType = BufferType.None;
        }

        public GPEntityConvert(BufferType wallBufferType)
        {
            _wallBufferType = wallBufferType;
        }

        public List<ThTCHWallData> WallDataDoorWindowRelation(List<ThTCHWallData> walls)
        {
            ThGPNTSSpatialIndex spatialIndex;
            wallDataDic = new Dictionary<WallGeometry, ThTCHWallData>();
            //var wallDataDic = new Dictionary<ThTCHMPolygon, ThTCHWallData>();
            addDBColl = new List<IBufferMessage>();
            wallCurves = new List<IBufferMessage>();
            foreach (var item in walls)
            {
                switch (item.CenterCurveCase)
                {
                    case ThTCHWallData.CenterCurveOneofCase.CenterLine:
                        {
                            wallDataDic.Add(BuildWallGemotry(item), item);
                            wallCurves.Add(item.CenterLine);
                            addDBColl.Add(item.BuildElement.Outline);
                            break;
                        }
                    case ThTCHWallData.CenterCurveOneofCase.CenterArc:
                        {
                            wallDataDic.Add(BuildWallGemotry(item), item);
                            wallCurves.Add(item.CenterArc);
                            addDBColl.Add(item.BuildElement.Outline);
                            break;
                        }
                    case ThTCHWallData.CenterCurveOneofCase.None:
                    default:
                        {
                            continue;
                        }
                }
            }
            spatialIndex = new ThGPNTSSpatialIndex(addDBColl);
            foreach (var entity in walls)
            {
                if (entity.CenterCurveCase != ThTCHWallData.CenterCurveOneofCase.None)
                {
                    if (entity.CenterCurveCase == ThTCHWallData.CenterCurveOneofCase.CenterLine)
                        GetWallPointOffSet(entity.CenterLine, wallCurves);
                    else if (entity.CenterCurveCase == ThTCHWallData.CenterCurveOneofCase.CenterArc)
                        GetWallPointOffSet(entity.CenterArc, wallCurves);
                }
                //if (Math.Abs(spLeftOffSet) > 0.001 || Math.Abs(epLeftOffSet) > 0.001 || Math.Abs(spRightOffSet) > 0.001 || Math.Abs(epRightOffSet) > 0.001)
                //{
                //    var dbWall = walls.Find(c => c.Id == entity.DBId);
                //    var tempEntity = DBToTHEntityCommon.TArchWallToEntityWall(dbWall, spLeftOffSet, epLeftOffSet, spRightOffSet, epRightOffSet, moveOffSet);
                //    if (tempEntity.Outline != null && tempEntity.Outline.Area > 100)
                //        wallDataDic.Add(entity.Outline, WallDataEntityToTCHWall(tempEntity));
                //}
                //else
                //{
                //    if (entity.Outline != null && entity.Outline.Area > 100)
                //        wallDataDic.Add(entity.Outline, WallDataEntityToTCHWall(entity));

                //}
            }
            foreach (var wallData in wallDataDic.Keys)
            {
                var wall = wallDataDic[wallData];
                wall.BuildElement.Outline = BuildWallOutLine(wallData);
            }
            return walls;
        }

        void GetWallPointOffSet(IBufferMessage wallCurve, List<IBufferMessage> otherWallCurves)
        {
            ThTCHPoint3d sp, ep;
            if (wallCurve is ThTCHLine wallLine)
            {
                sp = wallLine.StartPt;
                ep = wallLine.EndPt;
            }
            else if (wallCurve is ThTCHArc wallArc)
            {
                sp = wallArc.StartPt;
                ep = wallArc.EndPt;
            }
            else
            {
                throw new Exception();
            }
            double tolerance = 10;
            var wallData = wallDataDic.Keys.First(o => o.CenterCurve == wallCurve);
            var otherCurves = otherWallCurves.Where(c => c != wallCurve).ToList();
            var spCurves = PointGetCurves(sp, otherCurves, tolerance);
            if (spCurves.Count == 1)
            {
                var spWall = spCurves.Keys.First();
                var spWallData = wallDataDic.Keys.First(o => o.CenterCurve == spWall);
                WallBuffer(wallData, spWallData, spCurves.Values.First());
            }
            var epCurves = PointGetCurves(ep, otherCurves, tolerance);
            if (epCurves.Count == 1)
            {
                var epWall = epCurves.Keys.First();
                var epWallData = wallDataDic.Keys.First(o => o.CenterCurve == epWall);
                WallBuffer(wallData, epWallData, !epCurves.Values.First());
            }
        }

        Dictionary<IBufferMessage, bool> PointGetCurves(ThTCHPoint3d point, List<IBufferMessage> otherCurves, double tolerance = 1)
        {
            var resCurves = new Dictionary<IBufferMessage, bool>();
            foreach (var curve in otherCurves)
            {
                if (curve is ThTCHLine line)
                {
                    var curveSp = line.StartPt;
                    if (Math.Abs(curveSp.Z - point.Z) > 1)
                        continue;
                    if (curveSp.DistanceTo(point) < tolerance)
                    {
                        resCurves.Add(curve, true);
                    }
                    else if (line.EndPt.DistanceTo(point) < tolerance)
                    {
                        resCurves.Add(curve, false);
                    }
                }
                else if (curve is ThTCHArc arc)
                {
                    var curveSp = arc.StartPt;
                    if (Math.Abs(curveSp.Z - point.Z) > 1)
                        continue;
                    if (curveSp.DistanceTo(point) < tolerance)
                    {
                        resCurves.Add(curve, true);
                    }
                    else if (arc.EndPt.DistanceTo(point) < tolerance)
                    {
                        resCurves.Add(curve, false);
                    }
                }
            }
            return resCurves;
        }

        void WallBuffer(WallGeometry wall1, WallGeometry wall2, bool isSameDirection)
        {
            var wall1Curve1 = wall1.LeftCurve;
            var wall1Curve2 = wall1.RightCurve;
            IBufferMessage wall2Curve1, wall2Curve2;
            if (isSameDirection)
            {
                wall2Curve1 = wall2.RightCurve;
                wall2Curve2 = wall2.LeftCurve;
            }
            else
            {
                wall2Curve1 = wall2.LeftCurve;
                wall2Curve2 = wall2.RightCurve;
            }
            switch (_wallBufferType)
            {
                case BufferType.BUTT:
                    {
                        var intersectPt1 = wall1Curve1.ExtendCurve(500).Intersection(wall2Curve1.ExtendCurve(500));
                        var intersectPt2 = wall1Curve2.ExtendCurve(500).Intersection(wall2Curve2.ExtendCurve(500));
                        if (intersectPt1 == null || intersectPt2 == null)
                        {
                            //两个墙线没有交点，可以认为Join失败。
                            return;
                        }
                        else
                        {
                            //1
                            if (wall1Curve1 is ThTCHLine line)
                            {
                                if (intersectPt1.DistanceTo(line.StartPt) > intersectPt1.DistanceTo(line.EndPt))
                                {
                                    line.EndPt = intersectPt1;
                                }
                                else
                                {
                                    line.StartPt = intersectPt1;
                                }
                            }
                            else if (wall1Curve1 is ThTCHArc arc)
                            {

                            }
                            if (wall2Curve1 is ThTCHLine line2)
                            {
                                if (intersectPt1.DistanceTo(line2.StartPt) > intersectPt1.DistanceTo(line2.EndPt))
                                {
                                    line2.EndPt = intersectPt1;
                                }
                                else
                                {
                                    line2.StartPt = intersectPt1;
                                }
                            }
                            else if (wall2Curve1 is ThTCHArc arc2)
                            {

                            }
                            //2
                            if (wall1Curve2 is ThTCHLine line3)
                            {
                                if (intersectPt2.DistanceTo(line3.StartPt) > intersectPt2.DistanceTo(line3.EndPt))
                                {
                                    line3.EndPt = intersectPt2;
                                }
                                else
                                {
                                    line3.StartPt = intersectPt2;
                                }
                            }
                            else if (wall1Curve2 is ThTCHArc arc3)
                            {

                            }
                            if (wall2Curve2 is ThTCHLine line4)
                            {
                                if (intersectPt2.DistanceTo(line4.StartPt) > intersectPt2.DistanceTo(line4.EndPt))
                                {
                                    line4.EndPt = intersectPt2;
                                }
                                else
                                {
                                    line4.StartPt = intersectPt2;
                                }
                            }
                            else if (wall2Curve2 is ThTCHArc arc4)
                            {

                            }
                        }
                        break;
                    }
                case BufferType.MITTER:
                    {
                        var intersectPt1 = wall1Curve1.Intersection(wall2Curve1);
                        var intersectPt2 = wall1Curve2.Intersection(wall2Curve2);
                        if(intersectPt1 == null && intersectPt2 != null)
                        {
                            var extendWall1Curve1 = wall1Curve1.ExtendCurve(1000);
                            var extendWall2Curve1 = wall2Curve1.ExtendCurve(1000);
                            var extendWall1Curve2 = wall1Curve2.ExtendCurve(1000);
                            var pt1 = extendWall1Curve1.Intersection(extendWall2Curve1);
                            var pt2 = extendWall1Curve2.Intersection(extendWall2Curve1);
                            if(pt1 == null || pt2 == null)
                            {
                                //两个墙线没有交点，可以认为Join失败。
                                return;
                            }
                            //1
                            if (wall1Curve1 is ThTCHLine line)
                            {
                                if (pt1.DistanceTo(line.StartPt) > pt1.DistanceTo(line.EndPt))
                                {
                                    line.EndPt = pt1;
                                }
                                else
                                {
                                    line.StartPt = pt1;
                                }
                            }
                            else if (wall1Curve1 is ThTCHArc arc)
                            {

                            }
                            if (wall2Curve1 is ThTCHLine line2)
                            {
                                if (pt2.DistanceTo(line2.StartPt) > pt2.DistanceTo(line2.EndPt))
                                {
                                    line2.EndPt = pt2;
                                }
                                else
                                {
                                    line2.StartPt = pt2;
                                }
                            }
                            else if (wall2Curve1 is ThTCHArc arc2)
                            {

                            }
                            //2
                            if (wall1Curve2 is ThTCHLine line3)
                            {
                                if (pt2.DistanceTo(line3.StartPt) > pt2.DistanceTo(line3.EndPt))
                                {
                                    line3.EndPt = pt2;
                                }
                                else
                                {
                                    line3.StartPt = pt2;
                                }
                            }
                            else if (wall1Curve2 is ThTCHArc arc3)
                            {

                            }
                            if (wall2Curve2 is ThTCHLine line4)
                            {
                                if (intersectPt2.DistanceTo(line4.StartPt) > intersectPt2.DistanceTo(line4.EndPt))
                                {
                                    line4.EndPt = intersectPt2;
                                }
                                else
                                {
                                    line4.StartPt = intersectPt2;
                                }
                            }
                            else if (wall2Curve2 is ThTCHArc arc4)
                            {

                            }

                        }
                        else if(intersectPt1 != null && intersectPt2 == null)
                        {
                            var extendWall1Curve1 = wall1Curve1.ExtendCurve(1000);
                            var extendWall2Curve2 = wall2Curve2.ExtendCurve(1000);
                            var extendWall1Curve2 = wall1Curve2.ExtendCurve(1000);
                            var pt1 = extendWall1Curve1.Intersection(extendWall2Curve2);
                            var pt2 = extendWall1Curve2.Intersection(extendWall2Curve2);
                            if (pt1 == null || pt2 == null)
                            {
                                //两个墙线没有交点，可以认为Join失败。
                                return;
                            }
                            //1
                            if (wall1Curve1 is ThTCHLine line)
                            {
                                if (pt1.DistanceTo(line.StartPt) > pt1.DistanceTo(line.EndPt))
                                {
                                    line.EndPt = pt1;
                                }
                                else
                                {
                                    line.StartPt = pt1;
                                }
                            }
                            else if (wall1Curve1 is ThTCHArc arc)
                            {

                            }
                            if (wall2Curve1 is ThTCHLine line2)
                            {
                                if (intersectPt1.DistanceTo(line2.StartPt) > intersectPt1.DistanceTo(line2.EndPt))
                                {
                                    line2.EndPt = intersectPt1;
                                }
                                else
                                {
                                    line2.StartPt = intersectPt1;
                                }
                            }
                            else if (wall2Curve1 is ThTCHArc arc2)
                            {

                            }
                            //2
                            if (wall1Curve2 is ThTCHLine line3)
                            {
                                if (pt2.DistanceTo(line3.StartPt) > pt2.DistanceTo(line3.EndPt))
                                {
                                    line3.EndPt = pt2;
                                }
                                else
                                {
                                    line3.StartPt = pt2;
                                }
                            }
                            else if (wall1Curve2 is ThTCHArc arc3)
                            {

                            }
                            if (wall2Curve2 is ThTCHLine line4)
                            {
                                if (pt1.DistanceTo(line4.StartPt) > pt1.DistanceTo(line4.EndPt))
                                {
                                    line4.EndPt = pt1;
                                }
                                else
                                {
                                    line4.StartPt = pt1;
                                }
                            }
                            else if (wall2Curve2 is ThTCHArc arc4)
                            {

                            }
                        }
                        else
                        {
                            //两个墙线没有交点，可以认为Join失败。
                            return;
                        }
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        WallGeometry BuildWallGemotry(ThTCHWallData wall)
        {
            var wallGemoetry = new WallGeometry();
            switch (wall.CenterCurveCase)
            {
                case ThTCHWallData.CenterCurveOneofCase.CenterLine:
                    {
                        wallGemoetry.CenterCurve = wall.CenterLine;
                        wallGemoetry.LeftWidth = wall.LeftWidth;
                        wallGemoetry.RightWidth = wall.RightWidth;
                        var dir = wall.CenterLine.LineDirection();
                        var leftDir = dir.CrossProduct(ThProtoBufExtension.ZAxis).Normalized();
                        var rightDir = leftDir.Negated();
                        var leftStartPt = wall.CenterLine.StartPt.ToXbimPoint3D() + leftDir * wall.LeftWidth;
                        var leftEndPt = wall.CenterLine.EndPt.ToXbimPoint3D() + leftDir * wall.LeftWidth;
                        var rightStartPt = wall.CenterLine.StartPt.ToXbimPoint3D() + rightDir * wall.RightWidth;
                        var rightEndPt = wall.CenterLine.EndPt.ToXbimPoint3D() + rightDir * wall.RightWidth;
                        var leftCurve = new ThTCHLine();
                        leftCurve.StartPt = leftStartPt.XBimPoint2Point3D();
                        leftCurve.EndPt = leftEndPt.XBimPoint2Point3D();
                        var rightCurve = new ThTCHLine();
                        rightCurve.StartPt = rightStartPt.XBimPoint2Point3D();
                        rightCurve.EndPt = rightEndPt.XBimPoint2Point3D();
                        wallGemoetry.LeftCurve = leftCurve;
                        wallGemoetry.RightCurve = rightCurve;
                        break;
                    }
                case ThTCHWallData.CenterCurveOneofCase.CenterArc:
                    {
                        throw new ArgumentException();
                        break;
                    }
                case ThTCHWallData.CenterCurveOneofCase.None:
                default:
                    {
                        throw new ArgumentException();
                    }
            }
            return wallGemoetry;
        }

        ThTCHMPolygon BuildWallOutLine(WallGeometry wallGeometry)
        {
            var outLine = new ThTCHMPolygon();
            var shell = new ThTCHPolyline();
            uint index = 0;
            //segment 1
            if (wallGeometry.LeftCurve is ThTCHLine leftLine)
            {
                shell.Points.Add(leftLine.StartPt);
                shell.Points.Add(leftLine.EndPt);
                var segment = new ThTCHSegment();
                segment.Index.Add(index++);
                segment.Index.Add(index);
                shell.Segments.Add(segment);
            }
            else if(wallGeometry.LeftCurve is ThTCHArc leftArc)
            {
                shell.Points.Add(leftArc.StartPt);
                shell.Points.Add(leftArc.CenterPt);
                shell.Points.Add(leftArc.EndPt);
                var segment = new ThTCHSegment();
                segment.Index.Add(index++);
                segment.Index.Add(index++);
                segment.Index.Add(index);
                shell.Segments.Add(segment);
            }
            else
            {
                throw new NotSupportedException();
            }

            if (wallGeometry.RightCurve is ThTCHLine rightLine)
            {
                shell.Points.Add(rightLine.EndPt);
                shell.Points.Add(rightLine.StartPt);

                //segment 2
                var segment2 = new ThTCHSegment();
                segment2.Index.Add(index++);
                segment2.Index.Add(index);
                shell.Segments.Add(segment2);

                //segment 2
                var segment3 = new ThTCHSegment();
                segment3.Index.Add(index++);
                segment3.Index.Add(index);
                shell.Segments.Add(segment3);

                //segment 4
                var segment4 = new ThTCHSegment();
                segment4.Index.Add(index);
                segment4.Index.Add(0);
                shell.Segments.Add(segment4);
            }
            else if(wallGeometry.RightCurve is ThTCHArc rigthArc)
            {
                shell.Points.Add(rigthArc.EndPt);
                shell.Points.Add(rigthArc.CenterPt);
                shell.Points.Add(rigthArc.StartPt);

                //segment 2
                var segment2 = new ThTCHSegment();
                segment2.Index.Add(index++);
                segment2.Index.Add(index);
                shell.Segments.Add(segment2);

                //segment 2
                var segment3 = new ThTCHSegment();
                segment3.Index.Add(index++);
                segment3.Index.Add(index++);
                segment3.Index.Add(index);
                shell.Segments.Add(segment3);

                //segment 4
                var segment4 = new ThTCHSegment();
                segment4.Index.Add(index);
                segment4.Index.Add(0);
                shell.Segments.Add(segment4);
            }
            else
            {
                throw new NotSupportedException();
            }
            outLine.Shell = shell;
            return outLine;
        }

        class WallGeometry
        {
            public IBufferMessage CenterCurve { get; set; }
            public IBufferMessage LeftCurve { get; set; }
            public IBufferMessage RightCurve { get; set; }
            public double LeftWidth { get; set; }
            public double RightWidth { get; set; }
        }
    }
}
