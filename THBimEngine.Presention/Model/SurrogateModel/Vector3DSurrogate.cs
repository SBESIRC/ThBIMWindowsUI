using ProtoBuf;

namespace THBimEngine.Presention.Model.SurrogateModel
{
    [ProtoContract]
    public struct Vector3DSurrogate
    {
        public Vector3DSurrogate(double x, double y, double z) : this()
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        [ProtoMember(1)]
        public double X { get; set; }
        [ProtoMember(2)]
        public double Y { get; set; }
        [ProtoMember(3)]
        public double Z { get; set; }
    }
}
