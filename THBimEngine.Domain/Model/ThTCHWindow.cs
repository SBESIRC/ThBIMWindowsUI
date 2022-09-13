using ProtoBuf;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHWindow : ThTCHElement
    {
        [ProtoMember(21)]
        public uint WindowType { get; set; }

        private ThTCHWindow() { }
    }
}