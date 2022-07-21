using ProtoBuf;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHSite : ThTCHElement
    {
        [ProtoMember(11)]
        public ThTCHBuilding Building { get; set; }
    }
}
