using ProtoBuf;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHWindow: ThTCHElement
    {
        [ProtoMember(11)]
        public Point3DSurrogate CenterPoint { get; set; }
        [ProtoMember(12)]
        public double Width { get; set; }
        [ProtoMember(13)]
        public double Thickness { get; set; }
        [ProtoMember(14)]
        public Vector3DSurrogate XVector { get; set; }
        //X轴方向和宽度方向一致
        [ProtoMember(15)]
        public Vector3DSurrogate ExtrudedDirection { get; }
        private ThTCHWindow()
        {

        }
    }
}
