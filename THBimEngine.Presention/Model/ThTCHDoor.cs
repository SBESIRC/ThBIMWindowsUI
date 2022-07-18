using ProtoBuf;
using THBimEngine.Presention.Model.SurrogateModel;

namespace THBimEngine.Presention.Model
{
    [ProtoContract]
    public class ThTCHDoor //: ThIfcDoor
    {
        [ProtoMember(1)]
        public Point3DSurrogate CenterPoint { get; set; }
        [ProtoMember(2)]
        public double Width { get; set; }
        [ProtoMember(3)]
        public double Thickness { get; set; }
        [ProtoMember(4)]
        public Vector3DSurrogate ExtrudedDirection { get; }
        //X轴方向和宽度方向一致
        [ProtoMember(5)]
        public Point3DSurrogate XVector { get; set; }
        [ProtoMember(6)]
        public string OpenDirection { get; set; }
        private ThTCHDoor()
        {

        }
    }
}
