using Xbim.Ifc;
using Xbim.Ifc4.Kernel;
using System.Collections.Generic;
using Xbim.Ifc4.ProductExtension;

namespace ThBIMServer.Ifc4
{
    public static class ThProtoBuf2IFC4RelAggregatesFactory
    {
        public static void Create(IfcStore model, IfcBuilding building, List<IfcBuildingStorey> storeys)
        {
            using (var txn = model.BeginTransaction())
            {
                var ifcRel = model.Instances.New<IfcRelAggregates>();
                ifcRel.RelatingObject = building;
                storeys.ForEach(s => ifcRel.RelatedObjects.Add(s));
                txn.Commit();
            }
        }
    }
}
