using ProtoBuf;
using THBimEngine.Domain.GeometryModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHDescending
    {
        /// <summary>
        /// 降板高度
        /// </summary>
        [ProtoMember(21)]
        public double DescendingHeight { get; set; }

        /// <summary>
        /// 降板厚度
        /// </summary>
        [ProtoMember(22)]
        public double DescendingThickness { get; set; }

        /// <summary>
        /// 降板包围厚度
        /// </summary>
        [ProtoMember(23)]
        public double DescendingWrapThickness { get; set; }

        /// <summary>
        /// 是否是降板
        /// </summary>
        [ProtoMember(24)]
        public bool IsDescending { get; set; }

        /// <summary>
        /// 降板内轮廓线
        /// </summary>
        [ProtoMember(25)]
        public PolylineSurrogate Outline { get; set; }

        /// <summary>
        /// 降板外轮廓线
        /// </summary>
        [ProtoMember(26)]
        public PolylineSurrogate OutlineBuffer { get; set; }
    }
}
