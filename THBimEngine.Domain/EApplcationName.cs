using System.Collections.Generic;
using System.ComponentModel;

namespace THBimEngine.Domain
{
    /// <summary>
    /// 专业信息
    /// </summary>
    public enum EMajor
    {
        /// <summary>
        /// 结构
        /// </summary>
        [Description("结构")]
        Structure = 10,
        /// <summary>
        /// 建筑
        /// </summary>
        [Description("建筑")]
        Architecture = 20,
        /// <summary>
        /// 暖通
        /// </summary>
        [Description("暖通")]
        HVAC = 30,
        /// <summary>
        /// 电气
        /// </summary>
        [Description("电气")]
        Electrical = 40,
        /// <summary>
        /// 水
        /// </summary>
        [Description("水")]
        Water = 50,
    }
    /// <summary>
    /// 来源信息
    /// </summary>
    public enum EApplcationName 
    {
        /// <summary>
        /// CAD
        /// </summary>
        [Description("CAD")]
        CAD =10,
        /// <summary>
        /// SU
        /// </summary>
        [Description("SU")]
        SU =20,
        /// <summary>
        /// IFC
        /// </summary>
        [Description("IFC")]
        IFC =30,
        /// <summary>
        /// YDB
        /// </summary>
        [Description("YDB")]
        YDB =40,
    }

    public class SourceConfig 
    {
        public SourceConfig(EApplcationName source, string showName,string contain) 
        {
            Source = source;
            ShowName = showName;
            FileExt = new List<string>();
            LinkFileExt = new List<string>();
            DirNameContain = contain;
        }
        public EApplcationName Source { get; }
        public string DirNameContain { get; }
        public string ShowName { get; }
        public List<string> FileExt { get; set; }
        public List<string> LinkFileExt { get; set; }

    }
}
