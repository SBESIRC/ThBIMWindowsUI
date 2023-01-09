using Xbim.Ifc;

namespace XbimXplorer.Extensions.ModelConvert
{
    public interface IIfcModelConverter
    {
        IfcStore Convert(ThSUProjectData project);
        ThSUProjectData ReverseConvert(IfcStore store);
    }
}
