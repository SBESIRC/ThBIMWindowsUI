using Xbim.Ifc;
using MathNet.Spatial.Euclidean;
using Xbim.Ifc2x3.GeometryResource;
using ThBIMServer.Geometries;

namespace ThBIMServer.Ifc2x3
{
    public static class ThXbimIFC2x3Exetension
    {
        public static IfcDirection ToIfcDirection(this IfcStore model, Vector3D v)
        {
            return model.ToIfcDirection(v.ToXbimVector3D());
        }

        public static IfcCartesianPoint ToIfcCartesianPoint(this IfcStore model, Point3D p)
        {
            return model.ToIfcCartesianPoint(p.ToXbimPoint3D());
        }
    }
}
