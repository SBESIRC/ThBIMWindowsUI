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
            if(ifcElement is null)
            {
                return null;
            }
            var type = ifcElement.Representation.Representations.First().Items[0].ToString();

            if(type.Contains("Xbim.Ifc2x3.GeometricModelResource.IfcFacetedBrep"))
            {
                ;
                var ifcFacetedBrep = ifcElement.Representation.Representations.First().Items[0] as Xbim.Ifc2x3.GeometricModelResource.IfcFacetedBrep;
                var outerCurve = ifcFacetedBrep.Outer.CfsFaces.ToList();
                var geometryBrep = new GeometryBrep();
                foreach (var ifcFace in outerCurve)
                {
                    var pts = (ifcFace.Bounds.FirstOrDefault().Bound as Xbim.Ifc2x3.TopologyResource.IfcPolyLoop).Polygon.ToList();
                    geometryBrep.Outer.Add(pts.ToPlineSurrogate());
                }
                return geometryBrep;
            }
            var ifcExtrudedAreaSolid = ifcElement.Representation.Representations.First().Items[0] as Xbim.Ifc2x3.GeometricModelResource.IfcExtrudedAreaSolid;
            var height = ifcExtrudedAreaSolid.Depth;
            if (!(ifcExtrudedAreaSolid.SweptArea as Xbim.Ifc2x3.ProfileResource.IfcArbitraryClosedProfileDef is null))
            {

                //var outerCurve = (ifcExtrudedAreaSolid.SweptArea as Xbim.Ifc2x3.ProfileResource.IfcArbitraryClosedProfileDef).OuterCurve;
                ////if(outerCurve.ToString().Contains("IfcPolyline"))
                ////{
                ////    var pts = outerCurve
                ////}
                //    //as Xbim.Ifc2x3.GeometryResource.IfcCompositeCurve;
                //var zAxis = ifcExtrudedAreaSolid.ExtrudedDirection.XbimVector3D();
                //if (outerCurve.Segments.Count > 0)
                //    return new GeometryStretch(outerCurve.ToOutline(), new XbimVector3D(1, 0, 0), zAxis, height, ((Xbim.Ifc2x3.GeometryResource.IfcPlacement)((Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement)ifcElement.ObjectPlacement).RelativePlacement).Location.Z);
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

        public static GeometryParam SlabGeometryParam(this Xbim.Ifc2x3.SharedBldgElements.IfcSlab ifcElement, out List<GeometryStretch> slabDescendingData)
        {
            slabDescendingData = new List<GeometryStretch>();
            var ifcFaceBasedSurfaceModel = ifcElement.Representation.Representations.FirstOrDefault().Items[0] as Xbim.Ifc2x3.GeometricModelResource.IfcFaceBasedSurfaceModel;
            var cfsFaces = (ifcFaceBasedSurfaceModel.FbsmFaces).FirstOrDefault().CfsFaces;
            var geometryBrep = new GeometryBrep();
            foreach (var ifcFace in cfsFaces)
            {
                var pts = (ifcFace.Bounds.FirstOrDefault().Bound as Xbim.Ifc2x3.TopologyResource.IfcPolyLoop).Polygon.ToList();
                geometryBrep.Outer.Add(pts.ToPlineSurrogate());
            }
            return geometryBrep;
        }

        public static PolylineSurrogate ToPlineSurrogate(this List<Xbim.Ifc2x3.GeometryResource.IfcCartesianPoint> pts)
        {
            var pt3DSurrogates = new List<Point3DSurrogate>();
            foreach (var pt in pts)
            {
                pt3DSurrogates.Add(pt.ToPt3DSurrogate());
            }
            var pt3DCollectionSurrogate = new Point3DCollectionSurrogate(pt3DSurrogates);
            return new PolylineSurrogate(new List<Point3DCollectionSurrogate>() { pt3DCollectionSurrogate },true);
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

        public static PolylineSurrogate ToOutline(this List<Xbim.Ifc2x3.TopologyResource.IfcFace> outerCurve)
        {
            List<Point3DCollectionSurrogate> point3DCollectionSurrogate = new List<Point3DCollectionSurrogate>();
            foreach (Xbim.Ifc2x3.TopologyResource.IfcFace ifcFace in outerCurve)
            {
                var pts = (ifcFace.Bounds as Xbim.Ifc2x3.TopologyResource.IfcPolyLoop).Polygon;
                var pt3DSurrogates = new List<Point3DSurrogate>();
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    pt3DSurrogates.Add(pts[i].ToPt3DSurrogate());
                }
                var pt3DCollectionSurrogate = new Point3DCollectionSurrogate(pt3DSurrogates);
                point3DCollectionSurrogate.Add(pt3DCollectionSurrogate);
            }
            return new PolylineSurrogate(point3DCollectionSurrogate, true);
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



    }
}
