using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class ApplicationDefaultConfig
    {
        public static List<SourceConfig> DefaultConfig = new List<SourceConfig>
        {
            new SourceConfig(EApplcationName.CAD,"主体","CAD"){ FileExt =new List<string>{ ".ifc"},LinkFileExt =new List<string>{ ".ifc"} },
            new SourceConfig(EApplcationName.SU,"SU","SU") { FileExt =new List<string>{ ".skp"},LinkFileExt =new List<string>{ ".ifc"} },
            new SourceConfig(EApplcationName.IFC,"IFC","IFC") { FileExt =new List<string>{ ".ifc"},LinkFileExt =new List<string>{} } ,
            new SourceConfig(EApplcationName.YDB,"YDB","YDB") { FileExt =new List<string>{ ".ydb"},LinkFileExt =new List<string>{ ".ifc"} },
        };
        public static Dictionary<EMajor, string> GetMajorConfig() 
        {
            return EnumUtil.GetEnumDicDescriptions<EMajor>();
        }
    }
}
