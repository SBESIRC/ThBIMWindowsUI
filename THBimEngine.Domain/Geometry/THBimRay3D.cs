using Xbim.Common.Geometry;

namespace THBimEngine.Domain.Geometry
{
    public class THBimRay3D
    {
        public readonly XbimVector3D Direction;
        public readonly XbimPoint3D ThroughPoint;

        public THBimRay3D(XbimPoint3D throughPoint, XbimVector3D direction)
        {
            this.ThroughPoint = throughPoint;
            this.Direction = direction.Normalized();
        }

        public XbimPoint3D? IntersectionWith(ThBimPlane plane)
        {
            return plane.IntersectionWith(this);
        }
    }
}
