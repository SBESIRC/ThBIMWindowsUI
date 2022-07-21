using ProtoBuf;
using System.Collections.Generic;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHWall : ThTCHElement
    {
        /// <summary>
        /// 宽度
        /// </summary>
        [ProtoMember(11)]
        public double Width { get; set; }
        /// <summary>
        /// 长度
        /// </summary>
        [ProtoMember(12)]
        public double Length { get; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(13)]
        public Vector3DSurrogate ExtrudedDirection { get; private set; }
        /// <summary>
        /// 中线方向
        /// </summary>
        [ProtoMember(14)]
        public Vector3DSurrogate XVector { get; }
        /// <summary>
        /// 中线中点
        /// </summary>
        [ProtoMember(15)]
        public Point3DSurrogate Origin { get; }
        /// <summary>
        /// 门
        /// </summary>
        [ProtoMember(16)]
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        [ProtoMember(17)]
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        [ProtoMember(18)]
        public List<ThTCHOpening> Openings { get; private set; }

        private ThTCHWall()
        {

        }
    }
}
