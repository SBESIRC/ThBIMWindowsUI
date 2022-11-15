using Xbim.Ifc;
using System.Linq;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.ModelGeometry.Scene;
using Xbim.Common.Geometry;
using Newtonsoft.Json;
using System.IO;
using Xbim.Common.XbimExtensions;
using System.Collections.Generic;
using Xbim.Geometry.Engine.Interop;
using Xbim.Ifc2x3.GeometryResource;
using THBimEngine.Domain;
using Xbim.Ifc2x3.GeometricConstraintResource;

namespace XbimXplorer.Extensions
{
    public static class IFCReverse
    {
        /// <summary>
        /// IFCStore 逆转换为 ThTCHProjectData。
        /// 注：因为逆转换的局限性，此方法只支持由THBM自己生成的CAD版本IFC逆转换功能
        /// </summary>
        /// <param name="ifcStore"></param>
        /// <returns></returns>
        public static ThTCHProjectData ReverseCAD(IfcStore ifcStore)
        {
            if (ifcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                ThTCHProjectData project = new ThTCHProjectData();
                project.Root = new ThTCHRootData();
                var ifcProject = ifcStore.Instances.FirstOrDefault<IfcProject>();
                project.Root.GlobalId = ifcProject.Description??"";
                foreach (IfcSite ifcSite in ifcProject.Sites)
                {
                    var site = new ThTCHSiteData();
                    site.Root = new ThTCHRootData();
                    foreach (IfcBuilding ifcBuilding in ifcSite.Buildings)
                    {
                        var building = new ThTCHBuildingData();
                        building.Root = new ThTCHRootData();
                        building.Root.Name = ifcBuilding.Name;
                        foreach (IfcBuildingStorey ifcBuildingStorey in ifcBuilding.BuildingStoreys)
                        {
                            var BuildingStorey = new ThTCHBuildingStoreyData();
                            BuildingStorey.Number = ifcBuildingStorey.Name;
                            BuildingStorey.Elevation = ifcBuildingStorey.Elevation.Value;
                            BuildingStorey.BuildElement = new ThTCHBuiltElementData();
                            var ifcProperties = ifcStore.Instances.FirstOrDefault<IfcRelDefinesByProperties>(o => o.RelatedObjects.Contains(ifcBuildingStorey));
                            foreach (IfcPropertySingleValue pset in (ifcProperties.RelatingPropertyDefinition as IfcPropertySet).HasProperties)
                            {
                                BuildingStorey.BuildElement.Properties.Add(new ThTCHProperty()
                                {
                                    Key = pset.Name,
                                    Value = pset.NominalValue.Value.ToString(),
                                });
                            }
                            foreach (var item in ifcBuildingStorey.ContainsElements.Cast<IfcRelContainedInSpatialStructure>())
                            {
                                foreach (var ifcElement in item.RelatedElements)
                                {
                                    if(ifcElement is IfcWall ifcWall)
                                    {

                                    }
                                    else if (ifcElement is IfcSlab ifcSlab)
                                    {

                                    }
                                    else if (ifcElement is IfcRailing ifcRailing)
                                    {

                                    }
                                    else if (ifcElement is IfcSpace ifcRoom)
                                    {

                                    }
                                }
                            }
                            

                            building.Storeys.Add(BuildingStorey);
                        }
                        site.Buildings.Add(building);
                    }
                    project.Sites.Add(site);
                } 
            }
            return null;
        }

        /// <summary>
        /// IFCStore 逆转换为 ThSUProjectData。
        /// 注：因为逆转换的局限性，此方法只支持由THBM自己生成的CAD版本IFC逆转换功能
        /// </summary>
        public static ThSUProjectData ReverseSU(IfcStore ifcStore)
        {
            if (ifcStore.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                ThSUProjectData project = new ThSUProjectData();
                project.Root = new ThTCHRootData();
                var ifcProject = ifcStore.Instances.FirstOrDefault<IfcProject>();
                project.Root.GlobalId = ifcProject.Description??"";
                ThSUBuildingData buildingData = new ThSUBuildingData();
                foreach (IfcSite ifcSite in ifcProject.Sites)
                {
                    foreach (IfcBuilding ifcBuilding in ifcSite.Buildings)
                    {
                        foreach (IfcBuildingStorey ifcBuildingStorey in ifcBuilding.BuildingStoreys)
                        {
                            ThSUBuildingStoreyData storey = new ThSUBuildingStoreyData();
                            foreach (var item in ifcBuildingStorey.ContainsElements.Cast<IfcRelContainedInSpatialStructure>())
                            {
                                foreach (var ifcElement in item.RelatedElements)
                                {
                                    if (ifcElement is IfcWall ifcWall)
                                    {
                                        
                                    }
                                    else if (ifcElement is IfcSlab ifcSlab)
                                    {

                                    }
                                    else if (ifcElement is IfcRailing ifcRailing)
                                    {

                                    }
                                    else if (ifcElement is IfcSpace ifcRoom)
                                    {

                                    }
                                    IfcRepresentationItem body = ifcElement.Representation.Representations.FirstOrDefault().Items.FirstOrDefault();
                                    XbimGeometryEngine engine = new XbimGeometryEngine();
                                    IXbimSolid xbimSolid = null;
                                    if (body is Xbim.Ifc2x3.GeometricModelResource.IfcExtrudedAreaSolid solid)
                                    {
                                        xbimSolid = engine.CreateSolid(solid);
                                    }
                                    else if (body is Xbim.Ifc2x3.GeometricModelResource.IfcFacetedBrep brep)
                                    {
                                        xbimSolid = engine.CreateSolid(brep);
                                    }
                                    else
                                    {
                                        xbimSolid = null;
                                        throw new System.Exception();
                                    }
                                    ThSUCompDefinitionData compDef = new ThSUCompDefinitionData();
                                    foreach (var xbimface in xbimSolid.Faces)
                                    {
                                        ThSUFaceBrepData thSUFaceBrepData = new ThSUFaceBrepData();
                                        thSUFaceBrepData.OuterLoop = xbimface.OuterBound.Vertices.XBimVertexSet2SULoopData();
                                        foreach (var bound in xbimface.InnerBounds)
                                        {
                                            thSUFaceBrepData.InnerLoops.Add(bound.Vertices.XBimVertexSet2SULoopData());
                                        }
                                    }
                                    project.Definitions.Add(compDef);

                                    var index = project.Definitions.Count - 1;
                                    ThSUBuildingElementData element = new ThSUBuildingElementData();
                                    element.Component = new ThSUComponentData();
                                    element.Component.DefinitionIndex = index;
                                    IfcLocalPlacement placement = ifcElement.ObjectPlacement as IfcLocalPlacement;
                                    element.Component.Transformations = (placement.RelativePlacement as IfcAxis2Placement3D).IfcAxis2Placement3D2ThTCHMatrix3d();
                                    storey.Buildings.Add(element);
                                }
                            }
                            buildingData.Storeys.Add(storey);
                        }
                    }
                }
                project.Building = buildingData;
                return project;
            }
            return null;
        }
    }
}
