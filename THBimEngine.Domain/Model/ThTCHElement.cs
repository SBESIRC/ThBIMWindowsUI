using ProtoBuf;
using System;
using System.Collections.Generic;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public abstract class ThTCHElement
    {
        /*这里预留10个序列数据，外部序列数字冲11开始*/
        [ProtoMember(1)]
        public string Name { get; set; }
        public string Spec { get; set; }
        public string Useage { get; set; }
        [ProtoMember(2)]
        public string Uuid { get; set; }
        [ProtoMember(3)]
        public PolylineSurrogate Outline { get; set; }
        [ProtoMember(4)]
        public double Height { get; set; }
        public Dictionary<string, object> Properties { get; }
        public ThTCHElement()
        {
            Uuid = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, object>();
        }
    }
}
