using System;
using System.Linq;

using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.SharedBldgElements;

using NetTopologySuite.Geometries;
using ThBIMServer.NTS;
using THBimEngine.IO.Xbim;
using Xbim.Common.Geometry;


namespace THBimEngine.IO.NTS
{
    public static class ThNTSIfcProfileDefExtension
    {
        private readonly static XbimVector3D NEGATIVEZ = new XbimVector3D(0, 0, -1);

        public static Polygon ToPolygon(IfcWall wall)
        {
            if (wall.Representation.Representations[0].Items[0] is IfcSweptAreaSolid swept)
            {
                var ifcProf = swept.SweptArea;
                var localPalce = ((IfcLocalPlacement)wall.ObjectPlacement).RelativePlacement;

            }
            throw new NotSupportedException();
        }

        /// <summary>
        /// 应该在xbim原代码 ToxBimFace points里面去改
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="localPlacement"></param>
        /// <returns></returns>
        public static Polygon ToPolygon(this IfcProfileDef profile, IfcLocalPlacement localPlacement)
        {
            var pointsArray = profile.ToXbimFace(localPlacement).OuterBound.Points.ToArray();
            if (profile is IfcRectangleProfileDef)
            {
                pointsArray.Append(pointsArray[0]);
            }

            var lineString = pointsArray.ToLineString();
            if (lineString is LinearRing ring)
            {
                return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon(ring);
            }
            return ThIFCNTSService.Instance.GeometryFactory.CreatePolygon();
        }
    }
}
