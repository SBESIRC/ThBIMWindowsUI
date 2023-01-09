using Xbim.Ifc;
using Xbim.Common.Step21;

namespace XbimXplorer.Extensions.ModelConvert
{
    public class ThModelConvertService
    {
        private IIfcModelConverter Converter { get; set; }

        public ThModelConvertService(IfcSchemaVersion version)
        {
            if (version == IfcSchemaVersion.Ifc2X3)
            {
                Converter = new ThIfc2X3ModelConverter();
            }
            else if (version == IfcSchemaVersion.Ifc4)
            {
                Converter = new ThIfc4ModelConverter();
            }
        }

        public IfcStore ToIfcStore(ThSUProjectData suProject)
        {
            return Converter.Convert(suProject);
        }

        public ThSUProjectData ToSUProjectData(IfcStore store)
        {
            return Converter.ReverseConvert(store);
        }
    }
}
