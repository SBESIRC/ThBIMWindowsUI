using System;
using System.Linq;
using THBimEngine.IO.Xbim;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Interfaces;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;

namespace THBimEngine.IO.Geometry
{
    public class ThXbimGeometryAnalyzer
    {
        private readonly static XbimVector3D NEGATIVEZ = new XbimVector3D(0, 0, -1);

        public static IXbimFace WallBottomFace(IfcWall wall)
        {
            // Reference:
            //  https://thebuildingcoder.typepad.com/blog/2011/07/top-faces-of-wall.html
            //  https://thebuildingcoder.typepad.com/blog/2009/08/bottom-face-of-a-wall.html
            // The bottom face of the wall has a normal vector equal to (0,0,-1),
            // and in most cases this is the unique for the bottom face.
            // So we just iterate over all faces, and stop when we find one whose normal vector is vertical
            // and has a negative Z coordinate.
            if (wall.Representation.Representations[0].Items[0] is IfcExtrudedAreaSolid solid)
            {
                var xbimSolid = ThXbimGeometryService.Instance.Engine.Moved(
                    ThXbimGeometryService.Instance.Engine.CreateSolid(solid), wall.ObjectPlacement) as IXbimSolid;
                return xbimSolid.Faces.Where(f => f.Normal.Equals(NEGATIVEZ)).FirstOrDefault();
            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// 获取IFC Element的底面
        /// </summary>
        public static IXbimFace ElementBottomFace(IfcProduct ifcElement)
        {
            // Reference:
            //  https://thebuildingcoder.typepad.com/blog/2011/07/top-faces-of-wall.html
            //  https://thebuildingcoder.typepad.com/blog/2009/08/bottom-face-of-a-wall.html
            // The bottom face of the wall has a normal vector equal to (0,0,-1),
            // and in most cases this is the unique for the bottom face.
            // So we just iterate over all faces, and stop when we find one whose normal vector is vertical
            // and has a negative Z coordinate.

            var body = ifcElement.Representation.Representations[0].Items[0];
            var xbimSolid = ThXbimGeometryService.Instance.Engine.Moved(
                CreatXbimSolid(body), ifcElement.ObjectPlacement) as IXbimSolid;
            var bottomFace = xbimSolid.Faces.Where(f => f.Normal.Equals(NEGATIVEZ)).FirstOrDefault();
            if(bottomFace != null)
                return bottomFace;
            //世界坐标下的体，没有找到Normal为(0,0,-1)的face
            throw new NotSupportedException();
        }


        private static IXbimSolid CreatXbimSolid(IfcRepresentationItem body)
        {
            if (body is IIfcSweptAreaSolid solidIfcSweptAreaSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIfcSweptAreaSolid);
            }
            else if (body is IIfcExtrudedAreaSolid solidIfcExtrudedAreaSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIfcExtrudedAreaSolid);
            }
            else if (body is IIfcRevolvedAreaSolid solidIIfcRevolvedAreaSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcRevolvedAreaSolid);
            }
            else if (body is IIfcSweptDiskSolid solidIIfcSweptDiskSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcSweptDiskSolid);
            }
            else if (body is IIfcSurfaceCurveSweptAreaSolid solidIIfcSurfaceCurveSweptAreaSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcSurfaceCurveSweptAreaSolid);
            }
            else if (body is IIfcBooleanClippingResult solidIIfcBooleanClippingResult)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcBooleanClippingResult);
            }
            else if (body is IIfcHalfSpaceSolid solidIIfcHalfSpaceSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcHalfSpaceSolid);
            }
            else if (body is IIfcPolygonalBoundedHalfSpace solidIIfcPolygonalBoundedHalfSpace)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcPolygonalBoundedHalfSpace);
            }
            else if (body is IIfcBoxedHalfSpace solidIIfcBoxedHalfSpace)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcBoxedHalfSpace);
            }
            else if (body is IIfcCsgPrimitive3D solidIIfcCsgPrimitive3D)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcCsgPrimitive3D);
            }
            else if (body is IIfcCsgSolid solidIIfcCsgSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcCsgSolid);
            }
            else if (body is IIfcSphere solidIIfcSphere)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcSphere);
            }
            else if (body is IIfcBlock solidIIfcBlock)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcBlock);
            }
            else if (body is IIfcRightCircularCylinder solidIIfcRightCircularCylinder)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(solidIIfcRightCircularCylinder);
            }
            else if (body is IfcFacetedBrep brepSolid)
            {
                return ThXbimGeometryService.Instance.Engine.CreateSolid(brepSolid);
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
