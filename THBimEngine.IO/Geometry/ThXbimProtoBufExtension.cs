using Xbim.Common.Geometry;

namespace ThBIMServer.Geometries
{
    public static class ThXbimProtoBufExtension
    {
        public static ThTCHMatrix3d ToTCHMatrix3d(this XbimMatrix3D m)
        {
            return new ThTCHMatrix3d
            {
                Data11 = m.M11,
                Data12 = m.M12,
                Data13 = m.M13,
                Data14 = m.M14,
                Data21 = m.M21,
                Data22 = m.M22,
                Data23 = m.M23,
                Data24 = m.M24,
                Data31 = m.M31,
                Data32 = m.M32,
                Data33 = m.M33,
                Data34 = m.M34,
                Data41 = m.OffsetX,
                Data42 = m.OffsetY,
                Data43 = m.OffsetZ,
                Data44 = m.M44,
            };
        }

        public static XbimMatrix3D ToXbimMatrix3D(this ThTCHMatrix3d m)
        {
            return new XbimMatrix3D(
                m.Data11, m.Data12, m.Data13, m.Data14,
                m.Data21, m.Data22, m.Data23, m.Data24,
                m.Data31, m.Data32, m.Data33, m.Data34,
                m.Data41, m.Data42, m.Data43, m.Data44);
        }

        public static XbimVector3D ToXbimVector3D(this ThTCHVector3d v)
        {
            return new XbimVector3D(v.X, v.Y, v.Z);
        }

        public static XbimPoint3D ToXbimPoint3D(this ThTCHPoint3d p)
        {
            return new XbimPoint3D(p.X, p.Y, p.Z);
        }
    }
}
