using Xbim.Common.Geometry;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ProfileResource;
using Xbim.IO.Memory;
using THBimEngine.Domain;

namespace THBimEngine.Geometry
{
    static class ThIFC4GeExtension
    {
        public static IfcCartesianPoint ToIfcCartesianPoint(this MemoryModel model, XbimPoint3D point)
        {
            var pt = model.Instances.New<IfcCartesianPoint>();
            pt.SetXYZ(point.X, point.Y, point.Z);
            return pt;
        }

        public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this MemoryModel model, XbimPoint3D point)
        {
            var placement = model.Instances.New<IfcAxis2Placement3D>();
            placement.Location = ToIfcCartesianPoint(model, point);
            return placement;
        }
        public static IfcLocalPlacement ToIfcLocalPlacement(this MemoryModel model, XbimPoint3D origin, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = ToIfcAxis2Placement3D(model, origin);
            });
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this MemoryModel model, ThTCHPolyline e)
        {
            return model.Instances.New<IfcArbitraryClosedProfileDef>(d =>
            {
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.OuterCurve = ToIfcCompositeCurve(model, e);
            });
        }

        public static IfcRectangleProfileDef ToIfcRectangleProfileDef(this MemoryModel model, XbimPoint3D origin, double xDim, double yDim)
        {
            return model.Instances.New<IfcRectangleProfileDef>(d =>
            {
                d.XDim = xDim;
                d.YDim = yDim;
                d.ProfileType = IfcProfileTypeEnum.AREA;
                d.Position = ToIfcAxis2Placement2D(model, origin);
            });
        }

        public static IfcExtrudedAreaSolid ToIfcExtrudedAreaSolid(this MemoryModel model, IfcProfileDef profile, XbimVector3D direction, double depth)
        {
            return model.Instances.New<IfcExtrudedAreaSolid>(s =>
            {
                s.Depth = depth;
                s.SweptArea = profile;
                s.ExtrudedDirection = ToIfcDirection(model, direction);
                s.Position = ToIfcAxis2Placement3D(model, XbimPoint3D.Zero);
            });
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this MemoryModel model, XbimPoint3D point)
        {
            var placement = model.Instances.New<IfcAxis2Placement2D>();
            placement.Location = ToIfcCartesianPoint(model, point);
            return placement;
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this MemoryModel model, XbimPoint3D point, XbimVector3D direction)
        {
            return model.Instances.New<IfcAxis2Placement2D>(p =>
            {
                p.Location = ToIfcCartesianPoint(model, point);
                p.RefDirection = ToIfcDirection(model, direction);
            });
        }

        public static IfcDirection ToIfcDirection(this MemoryModel model, XbimVector3D vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXYZ(vector.X, vector.Y, vector.Z);
            return direction;
        }

        public static IfcCompositeCurve ToIfcCompositeCurve(this MemoryModel model, ThTCHPolyline polyline)
        {
            var compositeCurve = CreateIfcCompositeCurve(model);
            var pts = polyline.Points;
            foreach (var segment in polyline.Segments)
            {
                var curveSegement = CreateIfcCompositeCurveSegment(model);
                if (segment.Index.Count == 2)
                {
                    //直线
                    var poly = model.Instances.New<IfcPolyline>();
                    poly.Points.Add(ToIfcCartesianPoint(model, pts[segment.Index[0].ToInt()].Point3D2XBimPoint()));
                    poly.Points.Add(ToIfcCartesianPoint(model, pts[segment.Index[1].ToInt()].Point3D2XBimPoint()));
                    curveSegement.ParentCurve = poly;
                    compositeCurve.Segments.Add(curveSegement);
                }
                else
                {
                    //圆弧
                    var pt1 = pts[segment.Index[0].ToInt()].Point3D2XBimPoint();
                    var pt2 = pts[segment.Index[2].ToInt()].Point3D2XBimPoint();
                    var midPt = pts[segment.Index[1].ToInt()].Point3D2XBimPoint();
                    //计算圆心，半径
                    var seg1 = midPt - pt1;
                    var seg1Mid = pt1 + seg1.Normalized() * (midPt.PointDistanceToPoint(pt1) / 2);
                    var seg2 = midPt - pt2;
                    var seg2Mid = pt2 + seg2.Normalized() * (midPt.PointDistanceToPoint(pt2) / 2);
                    var faceNormal = THBimDomainCommon.ZAxis;
                    var mid1Dir = seg1.Normalized().CrossProduct(faceNormal);
                    var mid2Dir = seg2.Normalized().CrossProduct(faceNormal);
                    if (LineHelper.FindIntersection(seg1Mid, mid1Dir, seg2Mid, mid2Dir, out XbimPoint3D arcCenter) == 1)
                    {
                        bool isCl = seg1.Normalized().CrossProduct(seg2.Normalized().Negated()).Z > 0;
                        var radius = arcCenter.PointDistanceToPoint(pt1);
                        var trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
                        trimmedCurve.BasisCurve = model.Instances.New<IfcCircle>(c =>
                        {
                            c.Radius = radius;
                            c.Position = ToIfcAxis2Placement2D(model, arcCenter, THBimDomainCommon.XAxis);
                        });
                        trimmedCurve.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
                        trimmedCurve.SenseAgreement = isCl;
                        trimmedCurve.Trim1.Add(ToIfcCartesianPoint(model, pt1));
                        trimmedCurve.Trim2.Add(ToIfcCartesianPoint(model, pt2));
                        curveSegement.ParentCurve = trimmedCurve;
                        compositeCurve.Segments.Add(curveSegement);
                    }
                }
            }
            return compositeCurve;
        }
        public static XbimVector3D ToAcGePoint3d(this IfcCartesianPoint point)
        {
            return new XbimVector3D(point.X, point.Y, point.Z);
        }
        private static IfcCompositeCurve CreateIfcCompositeCurve(MemoryModel model)
        {
            return model.Instances.New<IfcCompositeCurve>();
        }
        private static IfcCompositeCurveSegment CreateIfcCompositeCurveSegment(MemoryModel model)
        {
            return model.Instances.New<IfcCompositeCurveSegment>(s =>
            {
                s.SameSense = true;
            });
        }

    }
}
