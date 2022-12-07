using System;
using System.Linq;
using THBimEngine.IO.Xbim;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometricModelResource;
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
    }
}
