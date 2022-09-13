using ProtoBuf;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHDoor : ThTCHElement
    {
        private ThTCHDoor() { }

        [ProtoMember(21)]
        public uint Swing { get; set; }

        [ProtoMember(22)]
        public uint OperationType { get; set; }
    }
}
