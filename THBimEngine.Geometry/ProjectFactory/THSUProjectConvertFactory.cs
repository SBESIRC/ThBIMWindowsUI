using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using Xbim.Common.Geometry;
using Xbim.Common.Step21;

namespace THBimEngine.Geometry.ProjectFactory
{
    public class THSUProjectConvertFactory : ConvertFactoryBase
    {
        public THSUProjectConvertFactory(IfcSchemaVersion ifcSchemaVersion) : base(ifcSchemaVersion)
        {
        }
        public override ConvertResult ProjectConvert(object objProject, bool createSolidMesh)
        {
            var project = objProject as ThSUProjectData;
            if (null == project)
                throw new System.NotSupportedException();
            ConvertResult convertResult = null;
            //step1 转换几何数据
            ThTCHProjectToTHBimProject(project);
            bimProject.ProjectIdentity = project.Root.GlobalId;
            if (createSolidMesh && !project.IsFaceMesh)
            {
                //CreateSolidMesh(allEntitys.Values.ToList());
            }
            foreach (var item in allStoreys)
            {
                bimProject.PrjAllStoreys.Add(item.Key, item.Value);
                foreach (var entity in item.Value.FloorEntitys)
                {
                    bimProject.PrjAllEntitys.Add(entity.Key, entity.Value);
                }
                foreach (var relation in item.Value.FloorEntityRelations)
                {
                    bimProject.PrjAllRelations.Add(relation.Key, relation.Value);
                }
            }
            convertResult = new ConvertResult(bimProject, allStoreys, allEntitys);
            return convertResult;
        }
        private void ThTCHProjectToTHBimProject(ThSUProjectData project)
        {
            bool MeshFlag = project.IsFaceMesh;
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project)
                return;
            bimProject = new THBimProject(CurrentGIndex(), project.Root.Name, "", project.Root.GlobalId);
            bimProject.ProjectIdentity = project.Root.GlobalId;
            var bimSite = new THBimSite(CurrentGIndex(), "", "", project.Root.GlobalId + "Site");//project.Site.Uuid 暂时SU还没有Site的概念，后续补充
            var bimBuilding = new THBimBuilding(CurrentGIndex(), project.Root.GlobalId + "BuildingName", "", project.Root.GlobalId + "BuildingUuid");//同理，暂时SU也没有Building的概念，后续补充
            foreach (var storey in project.Building.Storeys) //也暂时没有Storey的概念。。。
            {
                var bimStorey = new THBimStorey(CurrentGIndex(), storey.Number.ToString(), storey.Elevation, storey.Height, "", storey.Root.GlobalId);
                var suDefinitions = project.Definitions;
                bimStorey.Matrix3D = XbimMatrix3D.CreateTranslation(new XbimVector3D(0, 0, 0));
                {
                    //多线程有少数据导致后面报错，后续再处理
                    var moveVector = new XbimVector3D(0, 0, 0);
                    Parallel.ForEach(storey.Buildings, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, component =>
                    {
                        var componentId = CurrentGIndex();
                        THBimEntity bimComponent;
                        {
                            if (component.Component.IfcClassification.StartsWith("IfcWall"))
                            {
                                bimComponent = new THBimWall(componentId,
                            string.Format("component#{0}", "", componentId),
                            "",
                            MeshFlag ? null : suDefinitions[component.Component.DefinitionIndex].THSUGeometryParam(component.Component.Transformations),
                            "",
                            component.Root.GlobalId);
                            }
                            else if (component.Component.IfcClassification.StartsWith("IfcBeam"))
                            {
                                bimComponent = new THBimBeam(componentId,
                            string.Format("component#{0}", "", componentId),
                            "",
                            MeshFlag ? null : suDefinitions[component.Component.DefinitionIndex].THSUGeometryParam(component.Component.Transformations),
                            "",
                            component.Root.GlobalId);
                            }
                            else if (component.Component.IfcClassification.StartsWith("IfcColumn"))
                            {
                                bimComponent = new THBimColumn(componentId,
                            string.Format("component#{0}", "", componentId),
                            "",
                            MeshFlag ? null : suDefinitions[component.Component.DefinitionIndex].THSUGeometryParam(component.Component.Transformations),
                            "",
                            component.Root.GlobalId);
                            }
                            else if (component.Component.IfcClassification.StartsWith("IfcSlab"))
                            {
                                bimComponent = new THBimSlab(componentId,
                            string.Format("component#{0}", "", componentId),
                            "",
                            MeshFlag ? null : suDefinitions[component.Component.DefinitionIndex].THSUGeometryParam(component.Component.Transformations),
                            "",
                            component.Root.GlobalId);
                            }
                            else
                            {
                                bimComponent = new THBimUntypedEntity(componentId,
                            string.Format("component#{0}", "", componentId),
                            "",
                            MeshFlag ? null : suDefinitions[component.Component.DefinitionIndex].THSUGeometryParam(component.Component.Transformations),
                            "",
                            component.Root.GlobalId);
                                ((THBimUntypedEntity)bimComponent).EntityTypeName = "SU构件";
                            }
                        }
                        bimComponent.ParentUid = bimStorey.Uid;

                        var componentRelation = new THBimElementRelation(bimComponent.Id, bimComponent.Name, bimComponent, bimComponent.Describe, bimComponent.Uid);
                        lock (bimStorey)
                        {
                            bimStorey.FloorEntityRelations.Add(bimComponent.Uid, componentRelation);
                            bimStorey.FloorEntitys.Add(bimComponent.Uid, bimComponent);
                        }
                        lock (allEntitys)
                        {
                            allEntitys.Add(bimComponent.Uid, bimComponent);
                        }
                    });
                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                }
                allStoreys.Add(bimStorey.Uid, bimStorey);
                bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
            }
            bimSite.SiteBuildings.Add(bimBuilding.Uid, bimBuilding);
            bimProject.ProjectSite = bimSite;
        }
        private THBimOpening DoorWindowOpening(GeometryStretch doorWindowParam, double wallWidth, out THBimElementRelation openingRelation)
        {
            var cloneParam = doorWindowParam.Clone() as GeometryStretch;
            var openingId = CurrentGIndex();
            if (wallWidth > cloneParam.YAxisLength)
            {
                cloneParam.YAxisLength = wallWidth + 120;
            }
            var bimOpening = new THBimOpening(openingId, string.Format("opening#{0}", openingId), "", cloneParam);
            openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
            return bimOpening;
        }
    }
}
