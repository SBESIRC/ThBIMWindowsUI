using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain.Model.SurrogateModel;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public static class THBimDomainCommon
    {
        public static readonly XbimVector3D XAxis = new XbimVector3D(1, 0, 0);
        public static readonly XbimVector3D YAxis = new XbimVector3D(0, 1, 0);
        public static readonly XbimVector3D ZAxis = new XbimVector3D(0, 0, 1);

        public static XbimPoint3D Point3D2XBimPoint(this Point3DSurrogate point3DSurrogate) 
        {
            return new XbimPoint3D(point3DSurrogate.X, point3DSurrogate.Y, point3DSurrogate.Z);
        }
        public static XbimVector3D Vector3D2XBimVector(this Vector3DSurrogate vector3DSurrogate)
        {
            return new XbimVector3D(vector3DSurrogate.X, vector3DSurrogate.Y, vector3DSurrogate.Z);
        }
        public static XbimVector3D Point3D2Vector(this Point3DSurrogate point3DSurrogate) 
        {
            return new XbimVector3D(point3DSurrogate.X, point3DSurrogate.Y, point3DSurrogate.Z);
        }
        public static XbimVector3D Point3D2Vector(this XbimPoint3D point3D)
        {
            return new XbimVector3D(point3D.X, point3D.Y, point3D.Z);
        }
    }
}
