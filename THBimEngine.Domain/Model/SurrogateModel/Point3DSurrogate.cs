using ProtoBuf;

namespace THBimEngine.Domain.Model.SurrogateModel
{
    [ProtoContract]
    public struct Point3DSurrogate
    {
        public Point3DSurrogate(double x, double y, double z) : this()
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
