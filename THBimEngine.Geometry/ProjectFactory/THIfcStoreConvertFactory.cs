using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using THBimEngine.Domain;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

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
            THIfcStoreToTHBimProject(ifcStore);
            if (createSolidMesh)
            {
                CreateSolidMesh(allEntitys.Values.ToList());
            }
            //var projectEntitys = allEntitys.Where(c => c != null).ToDictionary(c => c.Uid, x => x);
            convertResult = new ConvertResult(bimProject, allStoreys, allEntitys);
            return convertResult;
        }

        private void THIfcStoreToTHBimProject(IfcStore ifcStore)
        {
            var project = ifcStore.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project) return;
            bimProject = new THBimProject(CurrentGIndex(), project.Name, "", project.GlobalId);
            bimProject.ProjectIdentity = project.GlobalId;
            var site = project.Sites.First() as Xbim.Ifc2x3.ProductExtension.IfcSite;
            var bimSite = new THBimSite(CurrentGIndex(), "", "", site.GlobalId);
            Dictionary<string, List<string>> elemntHoles =new Dictionary<string, List<string>>();
            var openings = GetOpeningsAndProjections(ifcStore);
            foreach (var building in site.Buildings)
            {
                var ifcBuilding = building as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
                var bimBuilding = new THBimBuilding(CurrentGIndex(), ifcBuilding.Name, "", ifcBuilding.GlobalId);
                foreach (var ifcStorey in ifcBuilding.BuildingStoreys)
                {
                    var storey = ifcStorey as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                    double Storey_Elevation = 0;
                    double Storey_Height = 3200;//0;
                    if (!(storey.Elevation is null))
                    {
                        Storey_Elevation = storey.Elevation.Value;
                        //高度需要做修改
                       //Storey_Height = double.Parse(((storey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                    }
                    var bimStorey = new THBimStorey(CurrentGIndex(), storey.Name, Storey_Elevation, Storey_Height, "", storey.GlobalId);
                    foreach (var spatialStructure in storey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        var ifcType = elements.First().ToString();
                        foreach (var item in elements) 
                        {
                            var ifcElem = item as IIfcElement;
                            //if (ifcElem.ContainedInStructure.Count() > 0)
                            //{
                            //    //关联关系
                            //    foreach (var connnect in ifcElem.ContainedInStructure) 
                            //    {
                            //        foreach (var reElem in connnect.RelatedElements) 
                            //        {
                            //            var railingRelation = new THBimElementRelation(reElem.EntityLabel, reElem.Name, reElem.GlobalId,bimStorey.Uid);
                            //            bimStorey.FloorEntityRelations.Add(railingRelation.Uid, railingRelation);
                            //        }
                            //    }
                            //}
                            //else 
                            //{

                            //}
                            //实体
                            var addEntity = new THBimIFCEntity(item);
                            addEntity.Uid = item.GlobalId;
                            addEntity.ParentUid = bimStorey.Uid;
                            allEntitys.Add(addEntity.Uid, addEntity);
                            var railingRelation = new THBimElementRelation(addEntity.Id, addEntity.Name, addEntity, addEntity.Describe, addEntity.Uid);
                            bimStorey.FloorEntityRelations.Add(addEntity.Uid, railingRelation);
                            bimStorey.FloorEntitys.Add(addEntity.Uid, addEntity);

                        }
                        
                        /*
                        if(ifcType.Contains("IfcWall"))
                        {
                            Parallel.ForEach(elements, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ifcWall =>
                            {
                                var wallId = CurrentGIndex();
                                var wall = ifcWall as Xbim.Ifc2x3.SharedBldgElements.IfcWall;
                                if(wall == null)
                                {
                                    return;
                                }
                                var bimWall = new THBimWall(wallId, string.Format("wall#{0}", wallId), wall.THIFCGeometryParam(), "", wall.GlobalId);
                                bimWall.ParentUid = bimStorey.Uid;
                                var wallRelation = new THBimElementRelation(bimWall.Id, bimWall.Name, bimWall, bimWall.Describe, bimWall.Uid);
                                lock (bimStorey)
                                {
                                    bimStorey.FloorEntityRelations.Add(bimWall.Uid, wallRelation);
                                    bimStorey.FloorEntitys.Add(bimWall.Uid, bimWall);
                                }
                                foreach (var opening in wall.Openings)
                                {
                                    var addEntitys = GetAddEntity(bimStorey.Uid, bimWall.Uid, opening, out List<THBimElementRelation> addRealion);
                                    if (addEntitys.Count > 0)
                                    {
                                        foreach (var entity in addEntitys)
                                        {
                                            if (entity is THBimOpening bimOpening)
                                            {
                                                bimWall.Openings.Add(bimOpening);
                                            }
                                            allEntitys.Add(entity);
                                            bimStorey.FloorEntitys.Add(entity.Uid, entity);
                                        }
                                    }
                                    foreach (var relation in addRealion)
                                    {
                                        bimStorey.FloorEntityRelations.Add(relation.Uid, relation);
                                    }
                                }
                                allEntitys.Add(bimWall);
                            });
                        }

                        else if(ifcType.Contains("Slab"))
                        {
                            Parallel.ForEach(elements, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ifcSlab =>
                            {
                                var slab = ifcSlab as Xbim.Ifc2x3.SharedBldgElements.IfcSlab;
                                var geoSlab = slab.SlabGeometryParam(out List<GeometryStretch> slabDescendingData);
                                var slabId = CurrentGIndex();
                                var bimSlab = new THBimSlab(slabId, string.Format("slab#{0}", slabId), geoSlab, "", slab.GlobalId);
                                bimSlab.ParentUid = bimStorey.Uid;
                                foreach (var item in slabDescendingData)
                                    bimSlab.SlabDescendingDatas.Add(item);
                                var slabRelation = new THBimElementRelation(bimSlab.Id, bimSlab.Name, bimSlab, bimSlab.Describe, bimSlab.Uid);
                                bimStorey.FloorEntityRelations.Add(bimSlab.Uid, slabRelation);
                                bimStorey.FloorEntitys.Add(bimSlab.Uid, bimSlab);
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimSlab);
                                }
                            });
                        }
                        else if(ifcType.Contains("Railing"))
                        {
                            Parallel.ForEach(elements, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, ifcRailing =>
                            {
                                var railingId = CurrentGIndex();
                                var railing = ifcRailing as Xbim.Ifc2x3.SharedBldgElements.IfcRailing;
                                var railingGeo = railing.THIFCGeometryParam() as GeometryStretch;
                                var bimRailing = new THBimRailing(railingId, string.Format("railing#{0}", railingId), railingGeo, "", railing.GlobalId);
                                bimRailing.ParentUid = bimStorey.Uid;
                                var railingRelation = new THBimElementRelation(bimRailing.Id, bimRailing.Name, bimRailing, bimRailing.Describe, bimRailing.Uid);
                                bimStorey.FloorEntityRelations.Add(bimRailing.Uid, railingRelation);
                                bimStorey.FloorEntitys.Add(bimRailing.Uid, bimRailing);
                                lock (allEntitys)
                                {
                                    allEntitys.Add(bimRailing);
                                }
                            });
                        }*/
                    }
                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                    allStoreys.Add(bimStorey.Uid, bimStorey);
                    bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
                }
                bimSite.SiteBuildings.Add(bimBuilding.Uid, bimBuilding);
            }
            Parallel.ForEach(openings, new ParallelOptions(), elementToFeatureGroup =>
            {
                if (allEntitys.TryGetValue(elementToFeatureGroup.Key.GlobalId, out THBimEntity entity)) 
                {
                    foreach (var opening in elementToFeatureGroup) 
                    {
                        if (allEntitys.ContainsKey(opening.GlobalId))
                        {
                            entity.Openings.Add(allEntitys[opening.GlobalId]);
                        }
                        else 
                        {
                            var addEntity = new THBimIFCEntity(opening);
                            addEntity.Uid = opening.GlobalId;
                            addEntity.ParentUid = entity.Uid;
                            allEntitys.Add(addEntity.Uid, addEntity);
                            entity.Openings.Add(addEntity);
                        }
                    }
                }
            });
            bimProject.ProjectSite = bimSite;
        }
        private List<THBimEntity> GetAddEntity(string storeyUid,string wallUid,Xbim.Ifc2x3.ProductExtension.IfcElement ifcElement,out List<THBimElementRelation> addRealtion) 
        {
            var addEntitys = new List<THBimEntity>();
            addRealtion = new List<THBimElementRelation>();
            var type = ifcElement.GetType().Name.ToLower();
            var uid = ifcElement.GlobalId;
            if (ifcElement is Xbim.Ifc2x3.ProductExtension.IfcOpeningElement ifcOpening) 
            {
                if (ifcOpening.HasFillings.Count() > 0)
                {
                    foreach (var fill in ifcOpening.HasFillings)
                    {
                        var buildElemnt = fill.RelatedBuildingElement;
                        var addE = GetAddEntity(storeyUid, wallUid, buildElemnt, out List<THBimElementRelation> addRel);
                        if (addE.Count > 0) 
                        {
                            addEntitys.AddRange(addE);
                            addRealtion.AddRange(addRel);
                        }
                    }
                }
            }
            if (type.Contains("hole") || type.Contains("open"))
            {
                var opningId = CurrentGIndex();
                var bimOpening = new THBimOpening(opningId, string.Format("opening#{0}", opningId), ifcElement.THIFCGeometryParam(), "", uid);
                bimOpening.ParentUid = wallUid;
                var openingRelation = new THBimElementRelation(bimOpening.Id, bimOpening.Name, bimOpening, bimOpening.Describe, bimOpening.Uid);
                openingRelation.ParentUid = storeyUid;
                addRealtion.Add(openingRelation);
                addEntitys.Add(bimOpening);
            }
            else if (type.Contains("window"))
            {
                var windowId = CurrentGIndex();
                var bimWindow = new THBimWindow(windowId, string.Format("window#{0}", windowId), ifcElement.THIFCGeometryParam(), "", uid);
                bimWindow.ParentUid = wallUid;
                var windowRelation = new THBimElementRelation(bimWindow.Id, bimWindow.Name, bimWindow, bimWindow.Describe, bimWindow.Uid);
                windowRelation.ParentUid = storeyUid;
                addRealtion.Add(windowRelation);
                addEntitys.Add(bimWindow);

            }
            else if (type.Contains("door"))
            {
                var doorId = CurrentGIndex();
                var bimDoor = new THBimDoor(doorId, string.Format("door#{0}", doorId), ifcElement.THIFCGeometryParam(), "", uid);
                bimDoor.ParentUid = wallUid;
                var doorRelation = new THBimElementRelation(bimDoor.Id, bimDoor.Name, bimDoor, bimDoor.Describe, bimDoor.Uid);
                doorRelation.ParentUid = storeyUid;
                addRealtion.Add(doorRelation);
                addEntitys.Add(bimDoor);
            }
            return addEntitys;
        }

        private List<IGrouping<IIfcElement, IIfcFeatureElement>> GetOpeningsAndProjections(IfcStore ifcStore) 
        {
            //srl change the way we handle aggregation as some ifc models now send them in multiple relationships
            //var compoundElementsDictionary =
            //    Model.Instances.OfType<IIfcRelAggregates>()
            //        .Where(x => x.RelatingObject is IIfcElement)
            //        .ToDictionary(x => x.RelatingObject, y => y.RelatedObjects);

            var compoundElementsDictionary = XbimMultiValueDictionary<IIfcObjectDefinition, IIfcObjectDefinition>.Create<HashSet<IIfcObjectDefinition>>();
            foreach (var aggRel in ifcStore.Instances.OfType<IIfcRelAggregates>())
            {
                try
                {
                    foreach (var relObj in aggRel.RelatedObjects)
                    {
                        compoundElementsDictionary.Add(aggRel.RelatingObject, relObj);
                    }
                }
                catch (Exception e)
                {
                    Logger.Error($"Problem in aggregates definition for #{aggRel.EntityLabel}", e);
                }
            }
            // openings
            var elementsWithFeatures = new List<ElementWithFeature>();
            var openingRelations = ifcStore.Instances.OfType<IIfcRelVoidsElement>()
                .Where(
                    r =>
                        r.RelatingBuildingElement != null && r.RelatingBuildingElement.Representation != null &&
                        r.RelatedOpeningElement != null && r.RelatedOpeningElement.Representation != null).ToList();
            foreach (var openingRelation in openingRelations)
            {
                // process parts
                ICollection<IIfcObjectDefinition> childrenElements;
                if (compoundElementsDictionary.TryGetValue(openingRelation.RelatingBuildingElement, out childrenElements))
                {
                    elementsWithFeatures.AddRange(
                        childrenElements.OfType<IIfcElement>().Select(childElement => new ElementWithFeature()
                        {
                            Element = childElement,
                            Feature = openingRelation.RelatedOpeningElement
                        }));
                }

                // process parent
                elementsWithFeatures.Add(new ElementWithFeature()
                {
                    Element = openingRelation.RelatingBuildingElement,
                    Feature = openingRelation.RelatedOpeningElement
                });
            }
            // projections
            var projectingRelations = ifcStore.Instances.OfType<IIfcRelProjectsElement>()
                .Where(
                    r =>
                        r.RelatingElement.Representation != null &&
                        r.RelatedFeatureElement.Representation != null).ToList();
            foreach (var projectionRelation in projectingRelations)
            {
                // process parts
                ICollection<IIfcObjectDefinition> childrenElements;
                if (compoundElementsDictionary.TryGetValue(projectionRelation.RelatingElement, out childrenElements))
                {
                    elementsWithFeatures.AddRange(
                        childrenElements.OfType<IIfcElement>().Select(childElement => new ElementWithFeature()
                        {
                            Element = childElement,
                            Feature = projectionRelation.RelatedFeatureElement
                        }));
                }

                // process parent
                elementsWithFeatures.Add(new ElementWithFeature()
                {
                    Element = projectionRelation.RelatingElement,
                    Feature = projectionRelation.RelatedFeatureElement
                });
            }
            var openingsAndProjections = elementsWithFeatures.GroupBy(x => x.Element, y => y.Feature).ToList();
            return openingsAndProjections;
        }
        private struct ElementWithFeature
        {
            internal IIfcElement Element;
            internal IIfcFeatureElement Feature;
        }
    }

}
