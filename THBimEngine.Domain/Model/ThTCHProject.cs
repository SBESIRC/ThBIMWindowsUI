using ProtoBuf;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHProject
    {
        [ProtoMember(1)]
        public string ProjectName { get; set; }
        [ProtoMember(2)]
        public ThTCHSite Site { get; set; }
    }
}
