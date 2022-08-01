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
        public override bool Equals(object obj)
        {
            if (null == obj || !(obj is Matrix3DSurrogate))
                return false;
            var otherMatrix = (Matrix3DSurrogate)obj;
            if (this.Data != null && otherMatrix.Data == null)
                return false;
            if (this.Data == null && otherMatrix.Data != null)
                return false;
            if (this.Data == null && otherMatrix.Data == null)
                return true;
            if (this.Data.Length != otherMatrix.Data.Length)
                return false;
            for (int i = 0; i < this.Data.Length; i++) 
            {
                var oldValue = this.Data[i];
                var otherValue = otherMatrix.Data[i];
                if (!oldValue.Equals(otherValue))
                    return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return Data.GetHashCode();
        }
    }
}
