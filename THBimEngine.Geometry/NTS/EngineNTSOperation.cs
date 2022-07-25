using NetTopologySuite.Operation.Buffer;
using THBimEngine.Domain.GeometryModel;
using NTSJoinStyle = NetTopologySuite.Operation.Buffer.JoinStyle;

namespace THBimEngine.Geometry.NTS
{
    public static class EngineNTSOperation
    {
        public static PolylineSurrogate BufferFlatPL(this PolylineSurrogate polyline, double distance)
        {
            var buffer = new BufferOp(polyline.ToNTSLineString(), new BufferParameters()
            {
                JoinStyle = NTSJoinStyle.Mitre,
            });
            return buffer.GetResultGeometry(distance).ToPolylineSurrogate();
        }
    }
}
