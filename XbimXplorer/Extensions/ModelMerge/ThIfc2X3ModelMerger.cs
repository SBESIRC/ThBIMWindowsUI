using System;
using System.Collections.Generic;
using Xbim.Ifc;

namespace XbimXplorer.Extensions.ModelMerge
{
    public class ThIfc2X3ModelMerger : IIfcModelMerger
    {
        public IfcStore Merge(IfcStore store, IfcStore project)
        {
            throw new NotImplementedException();
        }

        public IfcStore Merge(IfcStore store, ThSUProjectData project)
        {
            throw new NotImplementedException();
        }

        public IfcStore Merge(IfcStore store, List<ThSUProjectData> projects)
        {
            throw new NotImplementedException();
        }
    }
}
