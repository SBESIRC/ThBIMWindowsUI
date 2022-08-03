using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using Xbim.Common.Step21;
using Xbim.Ifc;

namespace THBimEngine.Geometry.ProjectFactory
{
    public class THIfcStoreConvertFactory : ConvertFactoryBase
    {
        public THIfcStoreConvertFactory(IfcSchemaVersion ifcSchemaVersion) : base(ifcSchemaVersion)
        {
        }
        public override ConvertResult ProjectConvert(object objProject, bool createSolidMesh)
        {
            var ifcStore = objProject as IfcStore;
            if (null == ifcStore)
                throw new System.NotSupportedException();
            ConvertResult convertResult = null;
            //step1 转换几何数据
            if(ifcStore.IfcSchemaVersion== IfcSchemaVersion.Ifc2X3)
            {
                THIfcStoreToTHBimProject2X3(ifcStore);
            }
            else
            {
                THIfcStoreToTHBimProject4(ifcStore);
            }
            if (createSolidMesh)
            {
                CreateSolidMesh(allEntitys);
            }
            var projectEntitys = allEntitys.Where(c => c != null).ToDictionary(c => c.Uid, x => x);
            convertResult = new ConvertResult(bimProject, allStoreys, projectEntitys);
            return convertResult;
        }

        private void THIfcStoreToTHBimProject2X3(IfcStore ifcStore)
        {
            var project = ifcStore.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project) return;
            bimProject = new THBimProject(CurrentGIndex(), project.Name, "", project.GlobalId);
            bimProject.ProjectIdentity = project.GlobalId;
            AddElementIndex();
            var site = project.Sites.First() as Xbim.Ifc2x3.ProductExtension.IfcSite;
            var bimSite = new THBimSite(CurrentGIndex(), "", "", site.GlobalId);
            AddElementIndex();

            foreach (var building in site.Buildings)
            {
                var ifcBuilding = building as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
                var bimBuilding = new THBimBuilding(CurrentGIndex(), ifcBuilding.Name, "", ifcBuilding.GlobalId);
                foreach (var ifcStorey in ifcBuilding.BuildingStoreys)
                {
                    var storey = ifcStorey as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                    double Storey_Elevation = 0;
                    double Storey_Height = 0;
                    if (!(storey.Elevation is null))
                    {
                        Storey_Elevation = storey.Elevation.Value;
                        Storey_Height = double.Parse(((storey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                    }

                    var bimStorey = new THBimStorey(CurrentGIndex(), storey.Name, Storey_Elevation, Storey_Height, "", storey.GlobalId);
                    AddElementIndex();

                    foreach (var spatialStructure in storey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        var ifcType = elements.First().ToString();
                        if(ifcType.Contains("IfcWall"))
                        {
                            var wall2 = elements.First() as Xbim.Ifc2x3.SharedBldgElements.IfcWall;
                            var outerCurve = ((wall2.Representation.Representations.First().Items[0] as Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid).SweptArea as Xbim.Ifc2x3.ProfileResource.IfcArbitraryClosedProfileDef).OuterCurve as Xbim.Ifc2x3.GeometryResource.IfcCompositeCurve;
                            var seg = outerCurve.Segments;
                            var height = (wall2.Representation.Representations.First().Items[0] as Xbim.Ifc2x3.GeometricModelResource.IfcExtrudedAreaSolid).Depth;
                            Parallel.ForEach(elements, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ifcWall =>
                            {
                                var wall = ifcWall as Xbim.Ifc2x3.SharedBldgElements.IfcWall;
                                var bimWall = new THBimWall(CurrentGIndex(), string.Format("wall#{0}", CurrentGIndex()), wall.THIFCGeometryParam(), "", wall.GlobalId);
                                bimWall.ParentUid = bimStorey.Uid;
                                var wallRelation = new THBimElementRelation(bimWall.Id, bimWall.Name, bimWall, bimWall.Describe, bimWall.Uid);
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimWall.Uid, wallRelation);
                                    bimStorey.FloorEntitys.Add(bimWall.Uid, bimWall);
                                }
                                AddElementIndex();

                                foreach (var opening in wall.Openings)
                                {
                                    var type = opening.Name.Value.ToString();
                                    var uid = opening.GlobalId;
                                    if (type.Contains("hole"))
                                    {
                                        var bimOpening = new THBimOpening(CurrentGIndex(), string.Format("opening#{0}", CurrentGIndex()), opening.THIFCGeometryParam(), "", uid);
                                        bimOpening.ParentUid = bimWall.Uid;
                                        var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
                                        openingRelation.ParentUid = storey.GlobalId;
                                        lock (bimStorey)
                                        {
                                            bimStorey.FloorEntityRelations.Add(bimOpening.Uid, openingRelation);
                                            bimStorey.FloorEntitys.Add(bimOpening.Uid, bimOpening);
                                        }
                                        lock (bimStorey)
                                        {
                                            allEntitys.Add(bimOpening);
                                        }
                                        bimWall.Openings.Add(bimOpening);
                                        AddElementIndex();
                                    }
                                    if (type.Contains("window"))
                                    {
                                        var bimWindow = new THBimWindow(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), opening.THIFCGeometryParam(), "", uid);
                                        bimWindow.ParentUid = bimWall.Uid;
                                        var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name, bimWindow, bimWindow.Describe, bimWindow.Uid);
                                        windowRelation.ParentUid = storey.GlobalId;
                                        lock (bimStorey)
                                        {
                                            bimStorey.FloorEntityRelations.Add(bimWindow.Uid, windowRelation);
                                            bimStorey.FloorEntitys.Add(bimWindow.Uid, bimWindow);
                                        }
                                        lock (allEntitys)
                                        {
                                            allEntitys.Add(bimWindow);
                                        }
                                        AddElementIndex();
                                        
                                    }
                                    if (type.Contains("door"))
                                    {
                                        var bimDoor = new THBimDoor(CurrentGIndex(), string.Format("door#{0}", CurrentGIndex()), opening.THIFCGeometryParam(), "", uid);
                                        bimDoor.ParentUid = bimWall.Uid;
                                        var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor, bimDoor.Describe, bimDoor.Uid);
                                        doorRelation.ParentUid = storey.GlobalId;
                                        lock (bimStorey)
                                        {
                                            bimStorey.FloorEntityRelations.Add(bimDoor.Uid, doorRelation);
                                            bimStorey.FloorEntitys.Add(bimDoor.Uid, bimDoor);
                                        }
                                        lock (allEntitys)
                                        {
                                            allEntitys.Add(bimDoor);
                                        }
                                        AddElementIndex();
                                    }
                                }

                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimWall);
                                }
                            });
                        }

                        if(ifcType.Contains("Slab"))
                        {
                            Parallel.ForEach(elements, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ifcSlab =>
                            {
                                var slab = ifcSlab as Xbim.Ifc2x3.SharedBldgElements.IfcSlab;
                                var geoSlab = slab.SlabGeometryParam(out List<GeometryStretch> slabDescendingData);
                                var bimSlab = new THBimSlab(CurrentGIndex(), string.Format("slab#{0}", CurrentGIndex()), geoSlab, "", slab.GlobalId);
                                bimSlab.ParentUid = bimStorey.Uid;
                                foreach (var item in slabDescendingData)
                                    bimSlab.SlabDescendingDatas.Add(item);
                                var slabRelation = new THBimElementRelation(bimSlab.Id, bimSlab.Name, bimSlab, bimSlab.Describe, bimSlab.Uid);
                                bimStorey.FloorEntityRelations.Add(bimSlab.Uid, slabRelation);
                                bimStorey.FloorEntitys.Add(bimSlab.Uid, bimSlab);
                                AddElementIndex();
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimSlab);
                                }
                            });
                        }

                        if(ifcType.Contains("Railing"))
                        {
                            Parallel.ForEach(elements, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ifcRailing =>
                            {
                                var railing = ifcRailing as Xbim.Ifc2x3.SharedBldgElements.IfcRailing;
                                var railingGeo = railing.THIFCGeometryParam() as GeometryStretch;
                                //if (railingGeo.OutLine.Points != null)
                                //    railingGeo.OutLine = railing.Outline.BufferFlatPL(railingGeo.YAxisLength / 2);
                                var bimRailing = new THBimRailing(CurrentGIndex(), string.Format("railing#{0}", CurrentGIndex()), railingGeo, "", railing.GlobalId);
                                bimRailing.ParentUid = bimStorey.Uid;
                                var railingRelation = new THBimElementRelation(bimRailing.Id, bimRailing.Name, bimRailing, bimRailing.Describe, bimRailing.Uid);
                                bimStorey.FloorEntityRelations.Add(bimRailing.Uid, railingRelation);
                                bimStorey.FloorEntitys.Add(bimRailing.Uid, bimRailing);
                                AddElementIndex();
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimRailing);
                                }
                            });
                        }
                    }

                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);

                    allStoreys.Add(bimStorey.Uid, bimStorey);
                    bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
                }
                bimSite.SiteBuildings.Add(bimBuilding.Uid, bimBuilding);
            }

            bimProject.ProjectSite = bimSite;
        }

        private void THIfcStoreToTHBimProject4(IfcStore ifcStore)
        {

        }
    }
}
