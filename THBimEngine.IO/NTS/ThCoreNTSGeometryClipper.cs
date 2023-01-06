using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using System.Collections.Generic;
using ThBIMServer.NTS;
using NTSGeometry = NetTopologySuite.Geometries.Geometry;

namespace THBimEngine.IO.NTS
{
    public class ThCoreNTSGeometryClipper
    {
        private Polygon Clipper { get; set; }

        public ThCoreNTSGeometryClipper(Polygon polygon)
        {
            Clipper = polygon;
        }

        public static List<NTSGeometry> Clip(Polygon polygon, LineString curve, bool inverted = false)
        {
            var clipper = new ThCoreNTSGeometryClipper(polygon);
            return clipper.Clip(curve, inverted);
        }

        public List<NTSGeometry> Clip(LineString curve, bool inverted = false)
        {
            return Clip(curve as NTSGeometry, inverted).ToGeometryCollection();
        }

        private NTSGeometry Clip(NTSGeometry other, bool inverted = false)
        {
            if (inverted)
            {
                var geos = OverlayNGRobust.Overlay(other, Clipper, SpatialFunction.Difference);
                if (geos.IsEmpty)
                {
                    geos = OverlayNGRobust.OverlaySR(other, Clipper, SpatialFunction.Difference);
                }
                return geos;
            }
            else
            {
                var geos = OverlayNGRobust.Overlay(Clipper, other, SpatialFunction.Intersection);
                if (geos.IsEmpty)
                {
                    geos = OverlayNGRobust.OverlaySR(Clipper, other, SpatialFunction.Intersection);
                }
                return geos;
            }
        }
    }
}
