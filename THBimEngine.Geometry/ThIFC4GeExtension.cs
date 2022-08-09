using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain;
using THBimEngine.Domain.GeometryModel;
using Xbim.Common.Geometry;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.TopologyResource;
using Xbim.IO.Memory;

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
            placement.Location = ToIfcCartesianPoint(model,point);
            return placement;
        }
        public static IfcLocalPlacement ToIfcLocalPlacement(this MemoryModel model, XbimPoint3D origin, IfcObjectPlacement relative_to = null)
        {
            return model.Instances.New<IfcLocalPlacement>(l =>
            {
                l.PlacementRelTo = relative_to;
                l.RelativePlacement = ToIfcAxis2Placement3D(model,origin);
            });
        }

        public static IfcArbitraryClosedProfileDef ToIfcArbitraryClosedProfileDef(this MemoryModel model, PolylineSurrogate e)
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
                d.Position = ToIfcAxis2Placement2D(model,origin);
            });
        }

        public static IfcExtrudedAreaSolid ToIfcExtrudedAreaSolid(this MemoryModel model, IfcProfileDef profile, XbimVector3D direction, double depth)
        {
            return model.Instances.New<IfcExtrudedAreaSolid>(s =>
            {
                s.Depth = depth;
                s.SweptArea = profile;
                s.ExtrudedDirection = ToIfcDirection(model,direction);
                s.Position = ToIfcAxis2Placement3D(model,XbimPoint3D.Zero);
            });
        }
        public static IfcFacetedBrepWithVoids ToIfcFacetedBrep(this MemoryModel model, List<PolylineSurrogate> facePlines, List<PolylineSurrogate> voidsFaces)
        {
            var facetedBrepWithVoids = model.Instances.New<IfcFacetedBrepWithVoids>();
            facetedBrepWithVoids.Outer = ToIfcClosedShell(model, facePlines);
            facetedBrepWithVoids.Voids.Add(ToIfcClosedShell(model, voidsFaces));
            return facetedBrepWithVoids;
        }
        
        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this MemoryModel model, XbimPoint3D point)
        {
            var placement = model.Instances.New<IfcAxis2Placement2D>();
            placement.Location = ToIfcCartesianPoint(model,point);
            return placement;
        }

        public static IfcAxis2Placement2D ToIfcAxis2Placement2D(this MemoryModel model, XbimPoint3D point, XbimVector3D direction)
        {
            return model.Instances.New<IfcAxis2Placement2D>(p =>
            {
                p.Location = ToIfcCartesianPoint(model,point);
                p.RefDirection = ToIfcDirection(model,direction);
            });
        }

        public static IfcDirection ToIfcDirection(this MemoryModel model, XbimVector3D vector)
        {
            var direction = model.Instances.New<IfcDirection>();
            direction.SetXYZ(vector.X, vector.Y, vector.Z);
            return direction;
        }

        public static IfcCompositeCurve ToIfcCompositeCurve(this MemoryModel model, PolylineSurrogate polyline)
        {
            var compositeCurve = CreateIfcCompositeCurve(model);
            for (int i = 0; i < polyline.Points.Count; i++)
            {
                var curveSegement = CreateIfcCompositeCurveSegment(model);
                var pt1 = polyline.Points[i].Points.First().Point3D2XBimPoint();
                XbimPoint3D pt2 = pt1;
                if (i + 1 < polyline.Points.Count)
                {
                    pt2 = polyline.Points[i + 1].Points.First().Point3D2XBimPoint();
                }
                else
                {
                    pt2 = polyline.Points[0].Points.First().Point3D2XBimPoint();
                }
                if (pt1.PointDistanceToPoint(pt2) < 1)
                    continue;
                if (polyline.Points[i].Points.Count != 1)
                {
                    //圆弧
                    var midPt = polyline.Points[i].Points.Last().Point3D2XBimPoint();
                    //poly.Points.Add(ToIfcCartesianPoint(model, midPt));
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
                else
                {
                    var poly = model.Instances.New<IfcPolyline>();
                    poly.Points.Add(ToIfcCartesianPoint(model, pt1));
                    poly.Points.Add(ToIfcCartesianPoint(model, pt2));
                    curveSegement.ParentCurve = poly;
                    compositeCurve.Segments.Add(curveSegement);
                }
            }
            return compositeCurve;
        }
        private static IfcClosedShell ToIfcClosedShell(this MemoryModel model, List<PolylineSurrogate> facePlines)
        {
            var ifcClosedShell = model.Instances.New<IfcClosedShell>();
            foreach (var face in facePlines)
            {
                ifcClosedShell.CfsFaces.Add(ToIfcFace(model, face));
            }
            return ifcClosedShell;
        }
        private static IfcFace ToIfcFace(this MemoryModel model, PolylineSurrogate facePLine)
        {
            var ifcFace = model.Instances.New<IfcFace>();
            ifcFace.Bounds.Add(ToIfcFaceBound(model, facePLine));
            return ifcFace;
        }
        private static IfcFaceBound ToIfcFaceBound(this MemoryModel model, PolylineSurrogate boundaryLoop)
        {
            return model.Instances.New<IfcFaceBound>(b =>
            {
                b.Bound = model.ToIfcPolyLoop(boundaryLoop);
            });
        }
        private static IfcPolyLoop ToIfcPolyLoop(this MemoryModel model, PolylineSurrogate boundaryLoop)
        {
            var polyLoop = model.Instances.New<IfcPolyLoop>();
            foreach (var points in boundaryLoop.Points)
            {
                foreach (var point in points.Points)
                    polyLoop.Polygon.Add(ToIfcCartesianPoint(model, point.Point3D2XBimPoint()));
            }
            return polyLoop;
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
