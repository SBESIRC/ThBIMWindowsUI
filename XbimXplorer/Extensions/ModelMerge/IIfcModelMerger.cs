using Xbim.Ifc;
using System.Collections.Generic;

namespace XbimXplorer.Extensions.ModelMerge
{
    public interface IIfcModelMerger
    {
        /// <summary>
        /// Merge IfcStore（相同IfcSchemaVersion）
        /// </summary>
        /// <param name="store"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        IfcStore Merge(IfcStore store, IfcStore project);

        /// <summary>
        /// Merge IfcStore（一个SU项目）
        /// </summary>
        /// <param name="store"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        IfcStore Merge(IfcStore store, ThSUProjectData project);

        /// <summary>
        /// Merge IfcStore（多个SU项目）
        /// </summary>
        /// <param name="store"></param>
        /// <param name="project"></param>
        /// <returns></returns>
        IfcStore Merge(IfcStore store, List<ThSUProjectData> projects);
    }
}
