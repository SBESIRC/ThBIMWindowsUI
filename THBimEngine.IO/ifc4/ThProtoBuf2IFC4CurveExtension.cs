﻿using Xbim.Ifc;
using Xbim.Common.Geometry;
using Xbim.Ifc4.GeometryResource;
using ThBIMServer.Geometries;
using System.Linq;
using Xbim.Ifc4.Interfaces;

namespace ThBIMServer.Ifc4
{
    public static class ThProtoBuf2IFC2x3CurveExtension
    {
        public static IfcCompositeCurve ToIfcCompositeCurve(this IfcStore model, ThTCHPolyline polyline)
        {
            var compositeCurve = ThIFC4Factory.CreateIfcCompositeCurve(model);
            var pts = polyline.Points;
            foreach (var segment in polyline.Segments)
            {
                var curveSegement = ThIFC4Factory.CreateIfcCompositeCurveSegment(model);
                if (segment.Index.Count == 2)
                {
                    //直线段
                    var startPt = pts[(int)segment.Index[0]];
                    var endPt = pts[(int)segment.Index[1]];
                    curveSegement.ParentCurve = model.ToIfcPolyline(startPt, endPt);
                    compositeCurve.Segments.Add(curveSegement);
                }
                else
                {
                    //圆弧段
                    var startPt = pts[(int)segment.Index[0]];
                    var midPt = pts[(int)segment.Index[1]];
                    var endPt = pts[(int)segment.Index[2]];
                    curveSegement.ParentCurve = model.ToIfcTrimmedCurve(startPt, midPt, endPt);
                    compositeCurve.Segments.Add(curveSegement);
                }
            }
            return compositeCurve;
        }
        
        public static IfcCompositeCurve ToIfcCompositeCurve(this IfcStore model, IXbimWire wire)
        {
            var compositeCurve = ThIFC4Factory.CreateIfcCompositeCurve(model);
            var pts = wire.Points.ToList();
            for (int i = 0; i < pts.Count; i++)
            {
                var curveSegement = ThIFC4Factory.CreateIfcCompositeCurveSegment(model);
                if(i != pts.Count - 1)
                    curveSegement.ParentCurve = model.ToIfcPolyline(pts[i], pts[i + 1]);
                else
                    curveSegement.ParentCurve = model.ToIfcPolyline(pts[i], pts[0]);
                compositeCurve.Segments.Add(curveSegement);
            }
            return compositeCurve;
        }

        private static IfcPolyline ToIfcPolyline(this IfcStore model, ThTCHPoint3d startPt, ThTCHPoint3d endPt)
        {
            var poly = model.Instances.New<IfcPolyline>();
            poly.Points.Add(model.ToIfcCartesianPoint(startPt));
            poly.Points.Add(model.ToIfcCartesianPoint(endPt));
            return poly;
        }

        private static IfcPolyline ToIfcPolyline(this IfcStore model, XbimPoint3D startPt, XbimPoint3D endPt)
        {
            var poly = model.Instances.New<IfcPolyline>();
            poly.Points.Add(model.ToIfcCartesianPoint(startPt));
            poly.Points.Add(model.ToIfcCartesianPoint(endPt));
            return poly;
        }

        private static IfcCircle ToIfcCircle(this IfcStore model, ThXbimCircle3D circle)
        {
            return model.Instances.New<IfcCircle>(c =>
            {
                c.Radius = circle.Geometry.Radius;
                c.Position = model.ToIfcAxis2Placement2D(circle.Geometry.CenterPoint.ToXbimPoint3D(), new XbimVector3D(0, 0, 1));
            });
        }

        private static IfcTrimmedCurve ToIfcTrimmedCurve(this IfcStore model, ThTCHPoint3d startPt, ThTCHPoint3d pt, ThTCHPoint3d endPt)
        {
            var trimmedCurve = model.Instances.New<IfcTrimmedCurve>();
            var circle = new ThXbimCircle3D(startPt.ToXbimPoint3D(), pt.ToXbimPoint3D(), endPt.ToXbimPoint3D());
            trimmedCurve.BasisCurve = model.ToIfcCircle(circle);
            trimmedCurve.MasterRepresentation = IfcTrimmingPreference.CARTESIAN;
            trimmedCurve.SenseAgreement = !circle.IsClockWise();
            trimmedCurve.Trim1.Add(model.ToIfcCartesianPoint(startPt));
            trimmedCurve.Trim2.Add(model.ToIfcCartesianPoint(endPt));
            return trimmedCurve;
        }
    }
}
