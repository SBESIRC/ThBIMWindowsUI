using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;

using THBimEngine.Domain;

namespace ThBIMServer.NTS
{
    public static class ThBimNTSExtension
    {

        public static Polygon ToNTSPolygon(this GeometryStretch geomStretch)
        {
            var geometry = geomStretch.ToNTSLineString();
            return geometry.CreatePolygon();
        }

        public static LineString ToNTSLineString(this GeometryStretch geomStretch)
        {
            if (geomStretch.Outline != null)
            {
                //这里默认xAxis一定是{1，0，0}
                var ls = geomStretch.Outline.Shell.ToNTSLineString();
                return ls;
            }
            else if (geomStretch.XAxisLength != 0)
            {
                throw new NotImplementedException();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

    }
}
