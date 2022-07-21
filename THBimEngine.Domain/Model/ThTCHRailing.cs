using ProtoBuf;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    [ProtoContract]
    public class ThTCHRailing:ThTCHElement
    {
        [ProtoMember(11)]
        public double Depth { get; set; }
        [ProtoMember(12)]
        public double Thickness { get; set; }
        [ProtoMember(13)]
        public Vector3DSurrogate ExtrudedDirection { get; set; }
    }
}
