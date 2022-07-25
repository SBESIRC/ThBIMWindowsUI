using ProtoBuf;

namespace THBimEngine.Domain.GeometryModel
{
    [ProtoContract]
    public struct Matrix3DSurrogate
    {
        public Matrix3DSurrogate(double[] data) : this()
        {
            this.Data = data;
        }

        [ProtoMember(1)]
        public double[] Data { get; set; }
    }
}
