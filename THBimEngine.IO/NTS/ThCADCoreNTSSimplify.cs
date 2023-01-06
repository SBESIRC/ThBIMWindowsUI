using NetTopologySuite.Geometries;
using NetTopologySuite.Simplify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThBIMServer.NTS;

namespace THBimEngine.IO.NTS
{
    public static class ThCADCoreNTSSimplify
    {
        public static LineString VWSimplify(this LineString pline, double distanceTolerance)
        {
            var simplifier = new VWLineSimplifier(pline.Coordinates, distanceTolerance);
            return ThIFCNTSService.Instance.GeometryFactory.CreateLineString(simplifier.Simplify());
        }

        public static LineString DPSimplify(this LineString pline, double distanceTolerance)
        {
            var simplifier = new DouglasPeuckerLineSimplifier(pline.Coordinates)
            {
                DistanceTolerance = distanceTolerance,
            };
            return ThIFCNTSService.Instance.GeometryFactory.CreateLineString(simplifier.Simplify());
        }

        public static LineString TPSimplify(this LineString pline, double distanceTolerance)
        {
            var result = TopologyPreservingSimplifier.Simplify(pline, distanceTolerance);
            if (result is LineString lineString)
            {
                return lineString;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
    }
}
