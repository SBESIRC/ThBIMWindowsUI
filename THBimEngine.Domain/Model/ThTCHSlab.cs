using ProtoBuf;
using System.Collections.Generic;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHSlab : ThTCHElement
    {
        /// <summary>
        /// 降板信息
        /// </summary>
        [ProtoMember(21)]
        public List<ThTCHDescending> Descendings { get; set; }

        private ThTCHSlab()
        {

        }
    }
}
