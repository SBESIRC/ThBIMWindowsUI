using ProtoBuf;

namespace THBimEngine.Domain.GeometryModel
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
        public override bool Equals(object obj)
        {
            if (null == obj || !(obj is Vector3DSurrogate))
                return false;
            var otherPoint = (Vector3DSurrogate)obj;
            if (!this.X.Equals(otherPoint.X) || !this.Y.Equals(otherPoint.Y) || !this.Z.Equals(otherPoint.Z))
                return false;
            return true;
        }
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }
    }
}
