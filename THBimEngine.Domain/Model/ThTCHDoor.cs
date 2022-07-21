using ProtoBuf;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHDoor: ThTCHElement
    {
        [ProtoMember(11)]
        public Point3DSurrogate CenterPoint { get; set; }
        [ProtoMember(12)]
        public double Width { get; set; }
        [ProtoMember(13)]
        public double Thickness { get; set; }
        [ProtoMember(14)]
        public Vector3DSurrogate ExtrudedDirection { get; }
        //X轴方向和宽度方向一致
        [ProtoMember(15)]
        public Point3DSurrogate XVector { get; set; }
        private ThTCHDoor()
        {

        }
    }
}
