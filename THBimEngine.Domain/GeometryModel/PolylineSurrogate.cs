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
    }
}
