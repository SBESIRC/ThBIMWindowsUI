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

        public override ConvertResult ProjectConvert(object project, bool createSolidMesh)
        {
            var ifcStore = project as IfcStore;
            if (null == ifcStore)
                return null;
            ReadDictionary(ifcStore);
            var dicEntitys = ReadGeomtry(ifcStore);
            var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            allEntitys.Clear();
            globalIndex = 0;
            if (null == project) 
                return null;
            bimProject = new THBimProject(CurrentGIndex(), ifcProject.Name, "", ifcProject.GlobalId);
            bimProject.ProjectIdentity = ifcStore.FileName;
            THBimSite bimSite = null;
            bool haveLoopup = shapeGeoLoopups != null && shapeGeoLoopups.Count > 0;
            foreach (var item in dicEntitys)
            {
                if (allEntitys.ContainsKey(item.Value.Uid))
                    continue;
                allEntitys.Add(item.Value.Uid, item.Value);
            }
            if (ifcProject.Sites != null && ifcProject.Sites.Count() > 0)
            {
                var site = ifcProject.Sites.First();
                bimSite = new THBimSite(CurrentGIndex(), site.Name, site.Description.ToString(), site.GlobalId);
                var addBuildings = IfcBuildingData(site.Buildings.ToList(), dicEntitys);
                foreach (var item in addBuildings)
                    bimSite.SiteBuildings.Add(item.Uid,item);
            }
            else 
            {
                bimSite = new THBimSite(CurrentGIndex(), "默认场地");
                if (ifcProject.Buildings != null && ifcProject.Buildings.Count() > 0) 
                {
                    var addBuildings = IfcBuildingData(ifcProject.Buildings.ToList(), dicEntitys);
                    foreach (var item in addBuildings)
                        bimSite.SiteBuildings.Add(item.Uid, item);
                }
            }
            
            bimProject.ProjectSite = bimSite;
            var convertResult = new ConvertResult(bimProject, allStoreys, allEntitys);
            return convertResult;
        }
        private List<THBimBuilding> IfcBuildingData(List<IIfcBuilding> allBuildings, Dictionary<int, THBimIFCEntity> dicEntitys) 
        {
            var resBuildings = new List<THBimBuilding>();
            foreach (var building in allBuildings)
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
                            if (!dicEntitys.ContainsKey(item.EntityLabel))
                                continue;
                            var addEntity = dicEntitys[item.EntityLabel];
                            var relation = new THBimElementRelation(addEntity.Id, addEntity.Name, addEntity, addEntity.Describe, addEntity.Uid);
                            bimStorey.FloorEntityRelations.Add(addEntity.Uid, relation);
                            bimStorey.FloorEntitys.Add(addEntity.Uid, addEntity);
                        }
                    }
                    prjEntityFloors.Add(bimStorey.Uid, bimStorey);
                    allStoreys.Add(bimStorey.Uid, bimStorey);
                    bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
                }
                if (building.ContainsElements.Count() > 0) 
                {
                    var bimStorey = new THBimStorey(CurrentGIndex(), "默认楼层", 0, 0);
                    foreach (var spatialStructure in building.ContainsElements) 
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0)
                            continue;
                        var ifcType = elements.First().ToString();
                        foreach (var item in elements)
                        {
                            var ifcElem = item as IIfcElement;
                            if (!dicEntitys.ContainsKey(item.EntityLabel))
                                continue;
                            var addEntity = dicEntitys[item.EntityLabel];
                            var relation = new THBimElementRelation(addEntity.Id, addEntity.Name, addEntity, addEntity.Describe, addEntity.Uid);
                            bimStorey.FloorEntityRelations.Add(addEntity.Uid, relation);
                            bimStorey.FloorEntitys.Add(addEntity.Uid, addEntity);
                        }
                    }
                    bimBuilding.BuildingStoreys.Add(bimStorey.Uid, bimStorey);
                }
                resBuildings.Add(bimBuilding);
            }
            return resBuildings;
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
                    geo.TempOriginDisplacement = XbimPoint3D.Zero;
                    addEntity.AllShapeGeometries.Add(new THBimShapeGeometry(geo, insModel.Transformation));
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
                    var insModel = shapeInstances.Find(c => c.ShapeGeometryLabel == geo.ShapeLabel);
                    if (insModel == null)
                        return;
                    var insModels = shapeGeoLoopups[geo.ShapeLabel];
                    if (insModels.Count < 1)
                        return;
                    geo.TempOriginDisplacement = XbimPoint3D.Zero;
                    foreach (var copyModel in insModels)
                    {
                        var ifcItem = ifcStore.Instances[copyModel.IfcProductLabel] as IIfcElement;
                        var addEntity = new THBimIFCEntity(ifcItem);
                        addEntity.Uid = ifcItem.GlobalId;
                        addEntity.AllShapeGeometries.Add(new THBimShapeGeometry(geo, copyModel.Transformation));
                        lock (allGeoEntitys)
                        {
                            if (!allGeoEntitys.ContainsKey(copyModel.IfcProductLabel))
                            {
                                allGeoEntitys.Add(copyModel.IfcProductLabel, addEntity);
                            }
                            else
                            {
                                var oldEntitys = allGeoEntitys[copyModel.IfcProductLabel];
                                oldEntitys.AllShapeGeometries.AddRange(addEntity.AllShapeGeometries);
                            }
                        }
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
