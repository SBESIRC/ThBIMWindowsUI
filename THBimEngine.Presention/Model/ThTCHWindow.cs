using ProtoBuf;
using THBimEngine.Presention.Model.SurrogateModel;

namespace THBimEngine.Presention.Model
{
    [ProtoContract]
    public class ThTCHWindow //: ThIfcWindow
    {
        [ProtoMember(1)]
        public Point3DSurrogate CenterPoint { get; set; }
        [ProtoMember(2)]
        public double Width { get; set; }
        [ProtoMember(3)]
        public double Thickness { get; set; }
        [ProtoMember(4)]
        public Vector3DSurrogate XVector { get; set; }
        //X轴方向和宽度方向一致
        [ProtoMember(5)]
        public Vector3DSurrogate ExtrudedDirection { get; }
        [ProtoMember(6)]
        public string OpenDirection { get; set; }

        private ThTCHWindow()
        {

        }
    }
}
