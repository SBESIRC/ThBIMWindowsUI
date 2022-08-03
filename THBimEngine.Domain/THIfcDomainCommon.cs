using System.Collections.Generic;
using System.Linq;
using THBimEngine.Domain.GeometryModel;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public static class THIfcDomainCommon
    {
        public static GeometryParam THIFCGeometryParam(this Xbim.Ifc2x3.Kernel.IfcProduct ifcElement)
        {
            var ifcExtrudedAreaSolid = ifcElement.Representation.Representations.First().Items[0] as Xbim.Ifc2x3.GeometricModelResource.IfcExtrudedAreaSolid;
            var height = ifcExtrudedAreaSolid.Depth;
            if (!(ifcExtrudedAreaSolid.SweptArea as Xbim.Ifc2x3.ProfileResource.IfcArbitraryClosedProfileDef is null))
            {
                var outerCurve = (ifcExtrudedAreaSolid.SweptArea as Xbim.Ifc2x3.ProfileResource.IfcArbitraryClosedProfileDef).OuterCurve as Xbim.Ifc2x3.GeometryResource.IfcCompositeCurve;
                var zAxis = ifcExtrudedAreaSolid.ExtrudedDirection.XbimVector3D();
                if (outerCurve.Segments.Count > 0)
                    return new GeometryStretch(outerCurve.ToOutline(), new XbimVector3D(1, 0, 0), zAxis, height);
            }
            else
            {
                var ifcRectangleProfileDef = ifcExtrudedAreaSolid.SweptArea as Xbim.Ifc2x3.ProfileResource.IfcRectangleProfileDef;
                var XDim = (double)ifcRectangleProfileDef.XDim.Value;
                var YDim = (double)ifcRectangleProfileDef.YDim.Value;
                var orginPt = ((Xbim.Ifc2x3.GeometryResource.IfcPlacement)((Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement)ifcElement.ObjectPlacement).RelativePlacement).Location.ToXbimPt();
                var XAxis = ifcRectangleProfileDef.Position.P[0];
                var ZAxis = XAxis.CrossProduct(ifcRectangleProfileDef.Position.P[1]);
                var outLineGeoParam = new GeometryStretch(orginPt, XAxis, XDim, YDim, ZAxis, height);
                return outLineGeoParam;
            }

            return null;
        }

        public static XbimPoint3D ToXbimPt(this Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint pt)
        {
            double X = 0, Y = 0, Z = 0;
            if (!double.IsNaN(pt.X))
            {
                X = pt.X;
            }
            if (!double.IsNaN(pt.Y))
            {
                Y = pt.Y;
            }
            if (!double.IsNaN(pt.Z))
            {
                Z = pt.Z;
            }

            return new XbimPoint3D(X, Y, Z);
        }

        public static PolylineSurrogate ToOutline(this Xbim.Ifc2x3.GeometryResource.IfcCompositeCurve outerCurve)
        {
            List<Point3DCollectionSurrogate> point3DCollectionSurrogate = new List<Point3DCollectionSurrogate>();
            foreach (Xbim.Ifc2x3.GeometryResource.IfcCompositeCurveSegment segment in outerCurve.Segments)
            {
                var pts = (segment.ParentCurve as Xbim.Ifc2x3.GeometryResource.IfcPolyline).Points;
                var pt3DSurrogates = new List<Point3DSurrogate>();
                for (int i=0;i < pts.Count-1;i++)
                {
                    pt3DSurrogates.Add(pts[i].ToPt3DSurrogate());
                }
                var pt3DCollectionSurrogate = new Point3DCollectionSurrogate(pt3DSurrogates);
                point3DCollectionSurrogate.Add(pt3DCollectionSurrogate);
            }
            return new PolylineSurrogate(point3DCollectionSurrogate,true);
        }

        public static Point3DSurrogate ToPt3DSurrogate(this Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint pt)
        {
            double X=0, Y=0, Z=0;
            if (!double.IsNaN(pt.X))
            {
                X = pt.X;
            }
            if (!double.IsNaN(pt.Y))
            {
                Y = pt.Y;
            }
            if (!double.IsNaN(pt.Z))
            {
                Z = pt.Z;
            }

            return new Point3DSurrogate(X,Y,Z);
        }

        public static GeometryParam SlabGeometryParam(this Xbim.Ifc2x3.SharedBldgElements.IfcSlab tchElement, out List<GeometryStretch> slabDescendingData)
        {
            slabDescendingData = new List<GeometryStretch>();
            return null;
            //var outLineGeoParam = new GeometryStretch(tchElement.Outline, XAxis,
            //                    ZAxis.Negated(),
            //                    tchElement.Height);
            //if (null != tchElement.Descendings)
            //{
            //    foreach (var item in tchElement.Descendings)
            //    {
            //        if (item.IsDescending)
            //        {
            //            //降板
            //            GeometryStretch desGeoStretch = new GeometryStretch(item.Outline, XAxis, ZAxis.Negated(), item.DescendingThickness, item.DescendingHeight);
            //            desGeoStretch.YAxisLength = item.DescendingWrapThickness;
            //            slabDescendingData.Add(desGeoStretch);
            //        }
            //        else
            //        {
            //            //洞口
            //            outLineGeoParam.OutLine.InnerPolylines.Add(item.Outline);
            //        }
            //    }
            //}
            //return outLineGeoParam;
        }



    }
}
