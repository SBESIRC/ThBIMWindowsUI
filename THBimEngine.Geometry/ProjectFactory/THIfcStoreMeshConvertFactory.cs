using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.Common.Metadata;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.IO.Memory;

namespace THBimEngine.Geometry.ProjectFactory
{
    public class THIfcStoreMeshConvertFactory : ConvertFactoryBase
    {
        /*
         IfcStore Mesh后转为THBimEntity
         */
        Dictionary<int, XbimShapeGeometry> shapeGeometries = new Dictionary<int, XbimShapeGeometry>();
        List<XbimShapeInstance> shapeInstances = new List<XbimShapeInstance>();
        IDictionary<int, List<XbimShapeInstance>> shapeGeoLoopups = new Dictionary<int, List<XbimShapeInstance>>();
        public THIfcStoreMeshConvertFactory(IfcSchemaVersion ifcSchemaVersion) : base(ifcSchemaVersion)
        {
        }

        public override ConvertResult ProjectConvert(object prject, bool createSolidMesh)
        {
            var ifcStore = prject as IfcStore;
            if (null == ifcStore)
                return null;
            ReadDictionary(ifcStore);
            var dicEntitys = ReadGeomtry(ifcStore);

            var project = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project) 
                return null;
            bimProject = new THBimProject(CurrentGIndex(), project.Name, "", project.GlobalId);
            bimProject.ProjectIdentity = ifcStore.FileName;
            var site = project.Sites.First();
            var bimSite = new THBimSite(CurrentGIndex(), "", "", site.GlobalId);
            bool haveLoopup = shapeGeoLoopups != null && shapeGeoLoopups.Count > 0;
            foreach (var building in site.Buildings)
            {
                var bimBuilding = new THBimBuilding(CurrentGIndex(), building.Name, "", building.GlobalId);
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    var storey = ifcStorey as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                    var bimStorey = IfcStoreyToBimStorey(storey);
                    foreach (var spatialStructure in storey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        var ifcType = elements.First().ToString();
                        foreach (var item in elements)
                        {
                            var ifcElem = item as IIfcElement;
                            if (!haveLoopup)
                            {
                                var insModel = shapeInstances.Find(c => c.IfcProductLabel == item.EntityLabel);
                                if (null == insModel)
                                    continue;
                                if (!dicEntitys.ContainsKey(insModel.ShapeGeometryLabel))
                                    continue;
                                var addEntity = dicEntitys[insModel.ShapeGeometryLabel];
                                if (!allEntitys.ContainsKey(addEntity.Uid))
                                {
                                    addEntity.ShapeGeometry.TempOriginDisplacement = XbimPoint3D.Zero;
                                    allEntitys.Add(addEntity.Uid, addEntity);
                                }
                                var railingRelation = new THBimElementRelation(addEntity.Id, addEntity.Name, addEntity, addEntity.Describe, addEntity.Uid);
                                railingRelation.Matrix3D = insModel.Transformation;
                                bimStorey.FloorEntityRelations.Add(addEntity.Uid, railingRelation);
                                bimStorey.FloorEntitys.Add(addEntity.Uid, addEntity);
                            }
                            else
                            {
                                var entityIns = ((Xbim.IO.Memory.InMemoryGeometryStore)ifcStore.GeometryStore).EntityInstanceLookup[ifcElem.EntityLabel];
                                foreach (var ins in entityIns)
                                {
                                    if (ins.IfcProductLabel != item.EntityLabel)
                                        continue;
                                    if (!dicEntitys.ContainsKey(ins.ShapeGeometryLabel))
                                        continue;
                                    var addEntity = dicEntitys[ins.ShapeGeometryLabel];
                                    if (!allEntitys.ContainsKey(addEntity.Uid))
                                    {
                                        addEntity.ShapeGeometry.TempOriginDisplacement = XbimPoint3D.Zero;
                                        allEntitys.Add(addEntity.Uid, addEntity);
                                    }
                                    var railingRelation = new THBimElementRelation(addEntity.Id, addEntity.Name, addEntity, addEntity.Describe, addEntity.Uid);
                                    railingRelation.Matrix3D = ins.Transformation;
                                    bimStorey.FloorEntityRelations.Add(addEntity.Uid, railingRelation);
                                    bimStorey.FloorEntitys.Add(addEntity.Uid, addEntity);
                                    break;
                                }
                            }
                        }
                    }
                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                    allStoreys.Add(bimStorey.Uid, bimStorey);
                    bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
                }
                bimSite.SiteBuildings.Add(bimBuilding.Uid, bimBuilding);
            }
            bimProject.ProjectSite = bimSite;
            var convertResult = new ConvertResult(bimProject, allStoreys, allEntitys);
            return convertResult;
        }

        private THBimStorey IfcStoreyToBimStorey(Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey storey) 
        {
            double storey_Elevation = 0;
            double storey_Height = 0;
            if (!(storey.Elevation is null))
            {
                storey_Elevation = storey.Elevation.Value;
            }
            var bimStorey = new THBimStorey(CurrentGIndex(), storey.Name, storey_Elevation, storey_Height, "", storey.GlobalId);
            if (storey.PropertySets == null || storey.PropertySets.Count() < 1)
                return bimStorey;
            foreach (var item in storey.PropertySets)
            {
                if (item.PropertySetDefinitions == null)
                    continue;
                foreach (var prop in item.PropertySetDefinitions)
                {
                    if (!(prop is IIfcPropertySet))
                        continue;
                    var propertySet = prop as IIfcPropertySet;
                    foreach (var realProp in propertySet.HasProperties)
                    {
                        if (realProp.Name == "Height")
                        {
                            if (realProp is IIfcPropertySingleValue propValue) 
                            {
                                if (double.TryParse(propValue.NominalValue.ToString(), out double height))
                                {
                                    storey_Height = height;
                                }
                            } 
                        }
                    }
                }
            }
            bimStorey.LevelHeight = storey_Height;
            return bimStorey;
        }
        private void ReadDictionary(IfcStore ifcStore) 
        {
            var excludedTypes = DefaultExclusions(ifcStore, null);
            using (var geomStore = ifcStore.GeometryStore)
            {
                if (geomStore is InMemoryGeometryStore meyGeoStore)
                {
                    shapeGeoLoopups = ((InMemoryGeometryStore)geomStore).GeometryShapeLookup;
                }
                using (var geomReader = geomStore.BeginRead())
                {
                    var tempIns = GetShapeInstancesToRender(geomReader, excludedTypes);
                    var geoCount = geomReader.ShapeGeometries.Count();
                    foreach (var item in geomReader.ShapeGeometries)
                    {
                        shapeGeometries.Add(item.ShapeLabel, item);
                    }
                    shapeInstances.AddRange(tempIns);
                }
            }
        }
        private Dictionary<int, THBimIFCEntity> ReadGeomtry(IfcStore ifcStore) 
        {
            var allGeoEntitys = new Dictionary<int, THBimIFCEntity>();
            if (shapeGeoLoopups == null || shapeGeoLoopups.Count < 1)
            {
                Parallel.ForEach(shapeGeometries.Values, new ParallelOptions(), geo =>
                {
                    var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == geo.ShapeLabel);
                    if (null == insModel)
                        return;
                    var ifcItem = ifcStore.Instances[insModel.IfcProductLabel] as IIfcElement;
                    var addEntity = new THBimIFCEntity(ifcItem);
                    addEntity.Uid = ifcItem.GlobalId;
                    addEntity.ShapeGeometry = geo;
                    lock (allGeoEntitys) 
                    {
                        allGeoEntitys.Add(geo.ShapeLabel, addEntity);
                    }
                });
            }
            else
            {
                Parallel.ForEach(shapeGeometries.Values, new ParallelOptions(), geo =>
                {
                    var insModels = shapeGeoLoopups[geo.ShapeLabel];
                    if (insModels.Count < 1)
                        return;
                    var ifcItem = ifcStore.Instances[insModels.First().IfcProductLabel] as IIfcElement;
                    var addEntity = new THBimIFCEntity(ifcItem);
                    addEntity.Uid = ifcItem.GlobalId;
                    addEntity.ShapeGeometry = geo;
                    lock (allGeoEntitys)
                    {
                        allGeoEntitys.Add(geo.ShapeLabel, addEntity);
                    }
                });
            }
            return allGeoEntitys;
        }

        IEnumerable<XbimShapeInstance> GetShapeInstancesToRender(IGeometryStoreReader geomReader, HashSet<short> excludedTypes)
        {
            var shapeInstances = geomReader.ShapeInstances
                .Where(s => s.RepresentationType == XbimGeometryRepresentationType.OpeningsAndAdditionsIncluded
                            &&
                            !excludedTypes.Contains(s.IfcTypeId));
            return shapeInstances;
        }
        HashSet<short> DefaultExclusions(IModel model, List<Type> exclude)
        {
            var excludedTypes = new HashSet<short>();
            if (exclude == null)
                exclude = new List<Type>()
                {
                    typeof(IIfcSpace),
                    typeof(IIfcFeatureElement)
                };
            foreach (var excludedT in exclude)
            {
                ExpressType ifcT;
                if (excludedT.IsInterface && excludedT.Name.StartsWith("IIfc"))
                {
                    var concreteTypename = excludedT.Name.Substring(1).ToUpper();
                    ifcT = model.Metadata.ExpressType(concreteTypename);
                }
                else
                    ifcT = model.Metadata.ExpressType(excludedT);
                if (ifcT == null) // it could be a type that does not belong in the model schema
                    continue;
                foreach (var exIfcType in ifcT.NonAbstractSubTypes)
                {
                    excludedTypes.Add(exIfcType.TypeId);
                }
            }
            return excludedTypes;
        }
    }
}
