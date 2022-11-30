using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Common.Geometry;
using Xbim.Common.Step21;

using THBimEngine.Domain;
using THBimEngine.Domain.Grid;
using THBimEngine.Domain.MidModel;


namespace THBimEngine.Geometry.ProjectFactory
{
    public class ThBimProjectDataConvertFactory : ConvertFactoryBase
    {
        public ThBimProjectDataConvertFactory(IfcSchemaVersion ifcSchemaVersion) : base(ifcSchemaVersion)
        {
        }
        public override ConvertResult ProjectConvert(object objProject, bool createSolidMesh)
        {
            ConvertResult convertResult = null;

            var prj = objProject as THBimProject;
            if (prj == null)
                throw new System.NotSupportedException();

            bimProject = prj;

            bimProject.PrjAllStoreys.Clear();
            bimProject.PrjAllEntitys.Clear();
            bimProject.PrjAllRelations.Clear();

            foreach (var item in bimProject.ProjectSite.SiteBuildings)
            {
                foreach (var storey in item.Value.BuildingStoreys)
                {
                    bimProject.PrjAllStoreys.Add(storey.Key, storey.Value);
                    allStoreys.Add(storey.Key, storey.Value);

                    foreach (var entity in storey.Value.FloorEntitys)
                    {
                        bimProject.PrjAllEntitys.Add(entity.Key, entity.Value);
                        allEntitys.Add(entity.Key, entity.Value);
                    }
                    foreach (var relation in storey.Value.FloorEntityRelations)
                    {
                        if (bimProject.PrjAllRelations.ContainsKey (relation.Key)==false )
                        {
                            bimProject.PrjAllRelations.Add(relation.Key, relation.Value);
                        }
                    }
                }
            }

            if (createSolidMesh)
            {
                CreateSolidMesh(allEntitys.Values.ToList());
            }

            convertResult = new ConvertResult(bimProject, allStoreys, allEntitys);

            return convertResult;
        }
    }
}
