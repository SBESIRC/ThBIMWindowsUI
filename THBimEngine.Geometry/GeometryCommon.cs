using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene.Extensions;

namespace THBimEngine.Geometry
{
    public static class GeometryCommon
    {
        public static XbimMatrix3D PlacementToMatrix3D(this IIfcObjectPlacement placement)
        {
            XbimMatrix3D matrix3D = XbimMatrix3D.Identity;
            if (placement == null)
            {
                return matrix3D;
            }
            var lp = placement as IIfcLocalPlacement;
            var gp = placement as IIfcGridPlacement;
            if (gp != null)
                throw new NotImplementedException("GridPlacement is not implemented");
            if (lp == null)
                return matrix3D;
            var ax3 = lp.RelativePlacement as IIfcAxis2Placement3D;
            if (ax3 != null)
                matrix3D = ax3.ToMatrix3D();
            else
            {
                var ax2 = lp.RelativePlacement as IIfcAxis2Placement2D;
                if (ax2 != null)
                    matrix3D = ax2.ToMatrix3D();
            }
            return matrix3D;
        }
    }
}
