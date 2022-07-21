using ProtoBuf;
using System.Collections.Generic;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHSlab: ThTCHElement
    {
        /// <summary>
        /// 降板信息
        /// </summary>
        [ProtoMember(11)]
        public List<ThTCHSlabDescendingData> Descendings { get; set; }

        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(12)]
        public Vector3DSurrogate ExtrudedDirection { get; private set; }

        private ThTCHSlab()
        {

        }
    }
}
