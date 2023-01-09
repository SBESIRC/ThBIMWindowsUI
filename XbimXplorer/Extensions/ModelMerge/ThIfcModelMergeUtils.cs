using System;
using Xbim.Ifc;
using Xbim.Common.Step21;
using XbimXplorer.Extensions.ModelConvert;
using System.Collections.Generic;

namespace XbimXplorer.Extensions.ModelMerge
{
    public static class ThIfcModelMergeUtils
    {
        public static IfcStore MergeIfcStores(List<IfcStore> stores)
        {
            throw new NotImplementedException();
        }

        public static IfcStore MergeIfcStore(IfcStore store, IfcStore project)
        {
            if (store.SchemaVersion == project.SchemaVersion)
            {
                if (store.SchemaVersion == IfcSchemaVersion.Ifc2X3)
                {
                    var merger = new ThIfc2X3ModelMerger();
                    return merger.Merge(store, project);
                }
                else if (store.IfcSchemaVersion == IfcSchemaVersion.Ifc4)
                {
                    var merger = new ThIfc4ModelMerger();
                    return merger.Merge(store, project);
                }
            }
            else
            {
                if (store.IfcSchemaVersion == IfcSchemaVersion.Ifc4 
                    && project.IfcSchemaVersion == IfcSchemaVersion.Ifc2X3)
                {
                    var merger = new ThIfc4ModelMerger();
                    var converter = new ThIfc2X3ModelConverter();
                    var model = converter.ReverseConvert(project);
                    if (model is ThSUProjectData sUProjectData)
                    {
                        return merger.Merge(store, sUProjectData);
                    }
                }
            }
            throw new NotSupportedException();
        }
    }
}
