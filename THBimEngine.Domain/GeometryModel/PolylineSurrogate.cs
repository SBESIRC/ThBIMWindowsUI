using ProtoBuf;
using System.Collections.Generic;

namespace THBimEngine.Domain.GeometryModel
{
    [ProtoContract]
    public struct PolylineSurrogate
    {
        public PolylineSurrogate(List<Point3DCollectionSurrogate> pts, bool closed) : this()
        {
            this.Points = pts;
            this.IsClosed = closed;
            this.InnerPolylines = new List<PolylineSurrogate>();
        }

        [ProtoMember(1)]
        public List<Point3DCollectionSurrogate> Points { get; set; }

        [ProtoMember(2)]
        public bool IsClosed { get; set; }
        [ProtoMember(3)]
        public List<PolylineSurrogate> InnerPolylines { get; set; }
        public override bool Equals(object obj)
        {
            if (null == obj || !(obj is PolylineSurrogate))
                return false;
            var other = (PolylineSurrogate)obj;
            if (other.IsClosed != this.IsClosed)
                return false;
            if (null != Points && null != other.Points)
            {
                if (Points.Count != other.Points.Count)
                    return false;
                for (int i = 0; i < Points.Count; i++) 
                {
                    var value = Points[i];
                    var otherValue = other.Points[i];
                    if (!value.Equals(otherValue))
                        return false;
                }
            }
            else if (null != Points && null == other.Points)
                return false;
            else if (null == Points && null != other.Points)
                return false;
            if (null != InnerPolylines && null != other.InnerPolylines)
            {
                if (InnerPolylines.Count != other.InnerPolylines.Count)
                    return false;
                for (int i = 0; i < InnerPolylines.Count; i++)
                {
                    var value = InnerPolylines[i];
                    var otherValue = other.InnerPolylines[i];
                    if (!value.Equals(otherValue))
                        return false;
                }
            }
            else if (null != InnerPolylines && null == other.InnerPolylines)
                return false;
            else if (null == InnerPolylines && null != other.InnerPolylines)
                return false;
            return true;
        }
        public override int GetHashCode()
        {
            return Points.GetHashCode() ^ InnerPolylines.GetHashCode();
        }
    }
}
