using System.Linq;
using Xbim.Common.Geometry;
using NetTopologySuite.Geometries;
using NetTopologySuite;
using System.Collections.Generic;
using THBimEngine.Domain;

namespace THBimEngine.IO.Xbim
{
    public static class ThXbimBrepExtension
    {
        private static readonly GeometryFactory GF = NtsGeometryServices.Instance.CreateGeometryFactory();

        public static Polygon ToPolygon(this IXbimFace face)
        {
            return GF.CreatePolygon(face.OuterBound.Points.ClosedPoints().ToLinearRing(),
                face.InnerBounds.Select(o => o.Points.ClosedPoints().ToLinearRing()).ToArray());
        }

        /// <summary>
        /// 闭合点集(NTS的ToLineString要求首尾点一致)
        /// </summary>
        /// <param name="pts"></param>
        /// <returns></returns>
        private static IEnumerable<XbimPoint3D> ClosedPoints(this IEnumerable<XbimPoint3D> pts)
        {
            if (pts.Any() && !pts.First().Equals(pts.Last()))
            {
                return pts.Append(pts.First());
            }
            return pts;
        }
    }
}
