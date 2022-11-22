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
using THBimEngine.Application;

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
            IfcStoreReadGeomtry2SUProject ifcStoreReadGeomtry2SUProject = new IfcStoreReadGeomtry2SUProject();
            var allGeoPointNormals = new List<PointNormal>();
            var ifcGeomtrys = ifcStoreReadGeomtry2SUProject.ReadGeomtry(ifcStore, out allGeoPointNormals);
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
                            storey.Root = new ThTCHRootData();
                            storey.Root.Name = ifcBuildingStorey.Name;
                            storey.Root.GlobalId = "ThDefinition" + ifcBuildingStorey.Name + ifcBuildingStorey.GlobalId;
                            foreach (var item in ifcBuildingStorey.ContainsElements.Cast<IfcRelContainedInSpatialStructure>())
                            {
                                foreach (var ifcElement in item.RelatedElements)
                                {
                                    var ifcElementEntityLable = ifcElement.EntityLabel.ToString();
                                    var ifcGeomtry = ifcGeomtrys.FirstOrDefault(o => o.EntityLable == ifcElementEntityLable);
                                    if (ifcGeomtry == null)
                                    {
                                        //do not
                                        continue;
                                    }
                                    else
                                    {
                                        ThSUCompDefinitionData compDef = new ThSUCompDefinitionData();
                                        foreach (var face in ifcGeomtry.Faces)
                                        {
                                            ThSUFaceMeshData thSUFaceMeshData = new ThSUFaceMeshData();
                                            var Mesh = new ThSUPolygonMesh();
                                            foreach (var polygon in face.faceTriangles)
                                            {
                                                var polygonIndex = Mesh.Points.Count;
                                                foreach (var ptIndex in polygon.ptIndex)
                                                {
                                                    var pt = allGeoPointNormals[ptIndex].Point;
                                                    Mesh.Points.Add(new ThTCHPoint3d() { X = pt.X, Y = pt.Z, Z = pt.Y });
                                                }
                                                var suPolygon = new ThSUPolygon();
                                                suPolygon.Indices.Add(polygonIndex);
                                                suPolygon.Indices.Add(polygonIndex + 1);
                                                suPolygon.Indices.Add(polygonIndex + 2);
                                                Mesh.Polygons.Add(suPolygon);
                                            }
                                            thSUFaceMeshData.Mesh = Mesh;
                                            compDef.MeshFaces.Add(thSUFaceMeshData);
                                        }
                                        project.Definitions.Add(compDef);

                                        var index = project.Definitions.Count - 1;
                                        ThSUBuildingElementData element = new ThSUBuildingElementData();
                                        element.Component = new ThSUComponentData();
                                        element.Component.DefinitionIndex = index;
                                        if (ifcElement is IfcWall)
                                        {
                                            element.Component.IfcClassification = "IfcWall";
                                        }
                                        else if (ifcElement is IfcWindow)
                                        {
                                            element.Component.IfcClassification = "IfcWindow";
                                        }
                                        else if (ifcElement is IfcDoor)
                                        {
                                            element.Component.IfcClassification = "IfcDoor";
                                        }
                                        if (ifcElement is IfcBeam)
                                        {
                                            element.Component.IfcClassification = "IfcBeam";
                                        }
                                        if (ifcElement is IfcColumn)
                                        {
                                            element.Component.IfcClassification = "IfcColumn";
                                        }
                                        else if (ifcElement is IfcSlab)
                                        {
                                            element.Component.IfcClassification = "IfcSlab";
                                        }
                                        else if (ifcElement is IfcSlab)
                                        {
                                            element.Component.IfcClassification = "IfcSlab";
                                        }
                                        else if (ifcElement is IfcRailing)
                                        {
                                            element.Component.IfcClassification = "IfcRailing";
                                        }
                                        storey.Buildings.Add(element);
                                    }
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
