using Xbim.Common.Geometry;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace THBimEngine.IO.Xbim
{
    public static class ThXbimIfcProfileDefExtension
    {
        public static IXbimFace ToXbimFace(this IfcProfileDef profileDef, IfcLocalPlacement localPlacement)
        {
            var face = ThXbimGeometryService.Instance.Engine.CreateFace(profileDef);
            return ThXbimGeometryService.Instance.Engine.Moved(face, localPlacement) as IXbimFace;
        }
    }
}
