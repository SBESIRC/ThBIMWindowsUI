using Xbim.Ifc;
using System.Collections.Generic;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.Ifc4.Interfaces;

namespace ThBIMServer.Ifc4
{
    public class ThProtoBuf2IFC4RelDefinesFactory
    {
        public static void RelDefinesByType2Wall(IfcStore model, List<IfcWall> walls, IfcWallTypeEnum wallType)
        {
            using (var txn = model.BeginTransaction())
            {
                var type = model.Instances.New<IfcWallType>(t =>
                {
                    t.PredefinedType = wallType;
                });
                walls.ForEach(w => w.AddDefiningType(type));
                txn.Commit();
            }
        }
    }
}
