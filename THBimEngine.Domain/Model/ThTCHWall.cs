using ProtoBuf;
using System.Collections.Generic;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHWall : ThTCHElement
    {
        /// <summary>
        /// 门
        /// </summary>
        [ProtoMember(21)]
        public List<ThTCHDoor> Doors { get; private set; }
        /// <summary>
        /// 窗
        /// </summary>
        [ProtoMember(22)]
        public List<ThTCHWindow> Windows { get; private set; }
        /// <summary>
        /// 开洞
        /// </summary>
        [ProtoMember(23)]
        public List<ThTCHOpening> Openings { get; private set; }

        private ThTCHWall()
        {

        }
    }
}
