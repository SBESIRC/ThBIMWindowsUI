using THBimEngine.Domain;
using THBimEngine.Domain.Model.SurrogateModel;
using Xbim.Common.Geometry;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.ProfileResource;
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

        //public static IfcAxis2Placement3D ToIfcAxis2Placement3D(this MemoryModel model, CoordinateSystem3d cs)
        //{
        //    return model.Instances.New<IfcAxis2Placement3D>(p =>
        //    {
        //        p.Axis = model.ToIfcDirection(cs.Zaxis);
        //        p.RefDirection = model.ToIfcDirection(cs.Xaxis);
        //        p.Location = model.ToIfcCartesianPoint(cs.Origin);
        //    });
        //}

        //public static IfcLocalPlacement ToIfcLocalPlacement(this MemoryModel model, CoordinateSystem3d cs, IfcObjectPlacement relative_to = null)
        //{
        //    return model.Instances.New<IfcLocalPlacement>(l =>
        //    {
        //        l.PlacementRelTo = relative_to;
        //        l.RelativePlacement = model.ToIfcAxis2Placement3D(cs);
        //    });
        //}

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
            foreach (var item in polyline.Points)
            {
                var curveSegement = CreateIfcCompositeCurveSegment(model);
                var poly = model.Instances.New<IfcPolyline>();
                foreach (var point in item.Points)
                {
                    poly.Points.Add(ToIfcCartesianPoint(model, point.Point3D2XBimPoint()));
                }
                curveSegement.ParentCurve = poly;
                compositeCurve.Segments.Add(curveSegement);
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
