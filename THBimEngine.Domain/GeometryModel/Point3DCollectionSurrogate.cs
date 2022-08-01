using ProtoBuf;
using System.Collections.Generic;

namespace THBimEngine.Domain.GeometryModel
{
    [ProtoContract]
    public struct Point3DCollectionSurrogate
    {
        public Point3DCollectionSurrogate(List<Point3DSurrogate> pts) : this()
        {
            this.Points = pts;
        }

        [ProtoMember(1)]
        public List<Point3DSurrogate> Points { get; set; }
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Point3DCollectionSurrogate))
                return false;
            var other = (Point3DCollectionSurrogate)obj;
            if (this.Points == null && other.Points!= null)
                return false;
            if (this.Points != null && other.Points == null)
                return false;
            if (this.Points == null && other.Points == null)
                return true;
            if (this.Points.Count != other.Points.Count)
                return false;
            for (int i = 0; i < this.Points.Count; i++) 
            {
                var oldValue = this.Points[i];
                var otherValue = other.Points[i];
                if (!oldValue.Equals(otherValue))
                    return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            return Points.GetHashCode();
        }
    }
}
