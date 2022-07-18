using ProtoBuf;

namespace THBimEngine.Presention.Model
{
    [ProtoContract]
    public class ThTCHSite //: ThIfcSite
    {
        [ProtoMember(1)]
        public ThTCHBuilding Building { get; set; }
    }
}
