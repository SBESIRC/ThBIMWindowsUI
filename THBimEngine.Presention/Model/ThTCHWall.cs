using ProtoBuf;
using System.Collections.Generic;
using THBimEngine.Presention.Model.SurrogateModel;

namespace THBimEngine.Presention.Model
{
    [ProtoContract]
    public class ThTCHWall //: ThIfcWall
    {
        /// <summary>
        /// 宽度
        /// </summary>
        [ProtoMember(1)]
        public double Width { get; set; }
        /// <summary>
        /// 高度
        /// </summary>
        [ProtoMember(2)]
        public double Height { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        [ProtoMember(3)]
        public double Length { get; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(4)]
        public Vector3DSurrogate ExtrudedDirection { get; private set; }
        /// <summary>
        /// 中线方向
        /// </summary>
        [ProtoMember(5)]
        public Vector3DSurrogate XVector { get; }
        /// <summary>
        /// 中线中点
        /// </summary>
        [ProtoMember(6)]
        public Point3DSurrogate Origin { get; }
        /// <summary>
        /// 门
        /// </summary>
        [ProtoMember(7)]
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        [ProtoMember(8)]
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        [ProtoMember(9)]
        public List<ThTCHOpening> Openings { get; private set; }

        private ThTCHWall()
        {

        }
    }
}
