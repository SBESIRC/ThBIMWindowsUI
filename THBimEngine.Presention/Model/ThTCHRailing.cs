using ProtoBuf;
using THBimEngine.Presention.Model.SurrogateModel;

namespace THBimEngine.Presention.Model
{
    /// <summary>
    /// 栏杆
    /// </summary>
    [ProtoContract]
    public class ThTCHRailing //: ThIfcRailing
    {
        [ProtoMember(1)]
        public double Depth { get; set; }
        [ProtoMember(2)]
        public double Thickness { get; set; }
        [ProtoMember(3)]
        public Vector3DSurrogate ExtrudedDirection { get; set; }
    }
}
