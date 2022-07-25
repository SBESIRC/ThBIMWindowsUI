using ProtoBuf;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHSite : ThTCHElement
    {
        [ProtoMember(21)]
        public ThTCHBuilding Building { get; set; }
    }
}
