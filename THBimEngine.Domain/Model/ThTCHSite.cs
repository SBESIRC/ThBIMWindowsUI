using ProtoBuf;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHSite //: ThIfcSite
    {
        [ProtoMember(1)]
        public ThTCHBuilding Building { get; set; }
    }
}
