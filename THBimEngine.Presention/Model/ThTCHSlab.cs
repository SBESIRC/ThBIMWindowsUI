﻿using ProtoBuf;
using System.Collections.Generic;
using THBimEngine.Presention.Model.SurrogateModel;

namespace THBimEngine.Presention.Model
{
    [ProtoContract]
    public class ThTCHSlab //: ThIfcSlab
    {
        /// <summary>
        /// 降板信息
        /// </summary>
        [ProtoMember(1)]
        public List<ThTCHSlabDescendingData> Descendings { get; set; }

        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(2)]
        public Vector3DSurrogate ExtrudedDirection { get; private set; }

        private ThTCHSlab()
        {

        }
    }
}
