using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace THBimEngine.Domain.MidModel
{
    public class TempModel
    {
        public List<Vec3> Points;
        public List<Buildingstorey> Buildingstoreys;
        public Dictionary<string, Component> Components;
        public List<Edge> Edges;
        public List<OutingPolygon> OutingPolygons;
        public List<UniComponent> UniComponents;

        public TempModel()
        {
            Points = new List<Vec3>();
            Buildingstoreys = new List<Buildingstorey>();
            Components = new Dictionary<string, Component>();
            Edges = new List<Edge>();
            OutingPolygons = new List<OutingPolygon>();
            UniComponents = new List<UniComponent>();
        }

        public void ModelConvert(THBimProject bimProject)
        {
            var ifcStore = bimProject.SourceProject as IfcStore;
            if (ifcStore != null)
            {
                if (ifcStore.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
                {
                    GetFromIfc2x3(ifcStore, bimProject);
                }
                else
                {
                    GetFromIfc4(ifcStore, bimProject);
                }
            }
            else
            {
                GetFromBimData(bimProject);
            }
        }

        public void AddProject(THBimProject bimProject)
        {
            var ifcStore = bimProject.SourceProject as IfcStore;
            if (ifcStore != null)
            {
                if (ifcStore.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
                {
                    GetFromIfc2x3(ifcStore, bimProject);
                }
                else
                {
                    AddFromIfc4(ifcStore, bimProject);
                }
            }
            else
            {
                GetFromBimData(bimProject);
            }
        }

        public void GetFromIfc2x3(IfcStore ifcStore, THBimProject bimProject)
        {
            int ptIndex = 0;//点索引
            int buildingIndex = 0;//建筑物索引
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            var allPoints = bimProject.AllGeoPointNormals(true);

            var ifcProject = ifcStore.Instances.FirstOrDefault<Xbim.Ifc4.Interfaces.IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();
            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    var storey = ifcStorey as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                    var buildingStorey = new Buildingstorey(storey, ref buildingIndex);
                    buildingStorey.element_index_s.Add(uniComponentIndex);

                    var height = GetIfcStoreyHeight(storey);
                    buildingStorey.height = height;
                    buildingStorey.top_elevation += height;
                    foreach (var spatialStructure in storey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        var ifcType = elements.First().ToString();
                        var type = ifcType.Split('.').Last();
                        var component = new Component(type, componentIndex);

                        if (!Components.ContainsKey(type))
                        {
                            Components.Add(type, component);
                            componentIndex++;
                        }

                        foreach (var item in elements)
                        {
                            var uid = item.EntityLabel.ToString();
                            var material = THBimMaterial.GetTHBimEntityMaterial(type, true);
                            if (bimProject.PrjAllEntitys.ContainsKey(uid))
                            {
                                material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true);
                            }
                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey, Components[type]);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            var triangles = allGeoModels[uid].FaceTriangles;
                            GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                            uniComponent.edge_ind_e = edgeIndex - 1;
                            uniComponent.tri_ind_e = triangleIndex - 1;

                            UniComponents.Add(uniComponent);
                        }
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(buildingStorey);
                }
            }
        }

        public void GetFromIfc4(IfcStore ifcStore, THBimProject bimProject)
        {
            int ptIndex = 0;//点索引
            int buildingIndex = 0;//建筑物索引
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引


            

            var allGeoModels = bimProject.AllGeoModels();
            var allPoints = bimProject.AllGeoPointNormals(true);

            var ifcProject = ifcStore.Instances.FirstOrDefault<Xbim.Ifc4.Interfaces.IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();

            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    int beamNum = 0;
                    var storey = ifcStorey as Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
                    var height = GetIfcStoreyHeight(storey);
                    var elevation = storey.Elevation.Value;
                    var buildingStorey = new Buildingstorey(storey, height, ref buildingIndex);
                    foreach (var spatialStructure in storey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        buildingStorey.element_index_s.Add(uniComponentIndex);
                        foreach (var item in elements)
                        {
                            var type = item.ToString().Split('.').Last();
                            if (item.Name.ToString().Contains("Beam_169763"))
                                beamNum++;
                            var component = new Component(type, componentIndex);

                            if (!Components.ContainsKey(type))
                            {
                                Components.Add(type, component);
                                componentIndex++;
                            }

                            var uid = item.EntityLabel.ToString();
                            var material = THBimMaterial.GetTHBimEntityMaterial(type, true);
                            if (bimProject.PrjAllEntitys.ContainsKey(uid))
                                material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true);
                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey, Components[type]);
                            GetProfileName(item, uniComponent);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                            }
                            else
                            {
                                ;
                            }

                            uniComponent.edge_ind_e = edgeIndex - 1;
                            uniComponent.tri_ind_e = triangleIndex - 1;
                            uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                            UniComponents.Add(uniComponent);

                        }
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(buildingStorey);
                }
            }
        }

        public void GetFromBimData(THBimProject bimProject)
        {
            int ptIndex = 0;//点索引
            int buildingIndex = 0;//建筑物索引
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            var allPoints = bimProject.AllGeoPointNormals(true);

            var typeLs = new List<string>();
            var storeys = bimProject.ProjectSite.SiteBuildings.Values.First().BuildingStoreys.Values;
            foreach (var type in typeLs)
            {
                var component = new Component(type, componentIndex);
                Components.Add(type, component);
                componentIndex++;
            }
            foreach (var storey in storeys)
            {
                var buildingStorey = new Buildingstorey(storey, ref buildingIndex);
                buildingStorey.element_index_s.Add(uniComponentIndex);

                foreach (var relation in storey.FloorEntityRelations.Values)
                {
                    var uid = relation.RelationElementUid;
                    var material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true);
                    var uniComponent = new UniComponent(relation, material, ref uniComponentIndex, buildingStorey);
                    UniComponents.Add(uniComponent);

                    uniComponent.edge_ind_s = edgeIndex;
                    uniComponent.tri_ind_s = triangleIndex;
                    var triangles = allGeoModels[relation.RelationElementUid].FaceTriangles;
                    GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                    uniComponent.edge_ind_e = edgeIndex - 1;
                    uniComponent.tri_ind_e = triangleIndex - 1;
                }
                buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                Buildingstoreys.Add(buildingStorey);
            }
        }

        public void AddFromIfc4(IfcStore ifcStore, THBimProject bimProject)
        {
            int ptIndex = Points.Count;//点索引
            int buildingIndex = 0;//建筑物索引
            int componentIndex = Components.Count;//属性索引(门、窗等)
            int edgeIndex = Edges.Count;//边索引
            int triangleIndex = OutingPolygons.Count;//三角面片索引
            int uniComponentIndex = UniComponents.Count;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            var allPoints = bimProject.AllGeoPointNormals(true);

            var ifcProject = ifcStore.Instances.FirstOrDefault<Xbim.Ifc4.Interfaces.IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();

            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    var storey = ifcStorey as Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
                    var height = GetIfcStoreyHeight(storey);
                    var elevation = storey.Elevation.Value;
                    var buildingStorey = new Buildingstorey(storey, height, ref buildingIndex);
                    foreach (var spatialStructure in storey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        buildingStorey.element_index_s.Add(uniComponentIndex);
                        foreach (var item in elements)
                        {
                            var type = item.ToString().Split('.').Last();
                            var component = new Component(type, componentIndex);

                            if (!Components.ContainsKey(type))
                            {
                                Components.Add(type, component);
                                componentIndex++;
                            }

                            var uid = item.EntityLabel.ToString();
                            var material = THBimMaterial.GetTHBimEntityMaterial(type, true);
                            if (bimProject.PrjAllEntitys.ContainsKey(uid))
                                material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true);
                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey, Components[type]);
                            GetProfileName(item, uniComponent);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                            }
                            else
                            {
                                ;
                            }

                            uniComponent.edge_ind_e = edgeIndex - 1;
                            uniComponent.tri_ind_e = triangleIndex - 1;
                            uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                            UniComponents.Add(uniComponent);

                        }
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(buildingStorey);
                }
            }
        }

        public void WriteMidFile()
        {
            string fileName = Path.Combine(System.IO.Path.GetTempPath(), "BimEngineData.get");
            FileStream fileStream = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            BinaryWriter writer = new BinaryWriter(fileStream, Encoding.UTF8);
            int cnt = OutingPolygons.Count;
            writer.Write(cnt);
            foreach (var trangle in OutingPolygons)
            {
                trangle.WriteToFile(writer, Points);
            }
            cnt = Edges.Count;
            writer.Write(cnt);
            foreach (var edge in Edges)
            {
                edge.WriteToFile(writer, Points);
            }
            cnt = Components.Count;
            writer.Write(cnt);
            foreach (var component in Components.Values)
            {
                component.WriteToFile(writer);
            }
            cnt = UniComponents.Count;
            writer.Write(cnt);
            foreach (var uniComponent in UniComponents)
            {
                uniComponent.WriteToFile(writer);
            }
            cnt = Buildingstoreys.Count;
            writer.Write(cnt);
            int index = 0;
            foreach (var storey in Buildingstoreys)
            {
                if (index == 12)
                {
                    ;
                }
                storey.WriteToFile(writer);
                index++;
            }
            writer.Close();
        }

        public double GetIfcStoreyHeight(Xbim.Ifc4.ProductExtension.IfcBuildingStorey storey)
        {
            if (null == storey || storey.PropertySets == null)
                return 0;
            foreach (var item in storey.PropertySets)
            {
                if (item.PropertySetDefinitions == null) continue;
                foreach (var prop in item.PropertySetDefinitions)
                {
                    if (!(prop is Xbim.Ifc4.Interfaces.IIfcPropertySet)) continue;
                    var propertySet = prop as Xbim.Ifc4.Interfaces.IIfcPropertySet;
                    foreach (var realProp in propertySet.HasProperties)
                    {
                        if (realProp.Name == "Height")
                        {
                            if (realProp is IIfcPropertySingleValue propValue)
                            {
                                if (double.TryParse(propValue.NominalValue.ToString(), out double height))
                                {
                                    return height;
                                }
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public void GetProfileName(Xbim.Ifc4.Kernel.IfcProduct ifcProduct, UniComponent uniComponent)
        {
            var profileName = "";
            double depth = 0;

            var item = ifcProduct.Representation.Representations.First().Items[0];
            var solid = item as Xbim.Ifc4.GeometricModelResource.IfcExtrudedAreaSolid;
            if (solid is null)
            {
                var rst = item as Xbim.Ifc4.GeometricModelResource.IfcBooleanResult;
                if(rst is null)
                {
                    return;
                }
                var solid2 = rst.FirstOperand as Xbim.Ifc4.GeometricModelResource.IfcSweptAreaSolid;
                profileName = solid2.SweptArea.ProfileName.ToString();
            }
            else
            {
                profileName = solid.SweptArea.ProfileName.ToString();
                depth = (double)solid.Depth.Value;
            }

            if (profileName.Contains("_") && profileName.Contains("*"))
            {
                string[] xyLen = profileName.Split('_')[1].Split('*');
                uniComponent.x_len = Convert.ToDouble(xyLen[0]);
                uniComponent.y_len = Convert.ToDouble(xyLen[1]);
            }
            else
            {
                ;
            }
            uniComponent.depth = depth;
            ;
        }


        public double GetIfcStoreyHeight(Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey storey)
        {
            foreach (var item in storey.PropertySets)
            {
                if (item.PropertySetDefinitions == null) continue;
                foreach (var prop in item.PropertySetDefinitions)
                {
                    if (!(prop is Xbim.Ifc4.Interfaces.IIfcPropertySet)) continue;
                    var propertySet = prop as Xbim.Ifc4.Interfaces.IIfcPropertySet;
                    foreach (var realProp in propertySet.HasProperties)
                    {
                        if (realProp.Name == "Height")
                        {
                            if (realProp is IIfcPropertySingleValue propValue)
                            {
                                if (double.TryParse(propValue.NominalValue.ToString(), out double height))
                                {
                                    return height;
                                }
                            }
                        }
                    }
                }
            }
            return 0;
        }

        public List<Edge> GetEdges(OutingPolygon outingPolygon, List<PointNormal> allPoints, ref int edgeIndex, int parentId)
        {
            var edges = new List<Edge>();
            var tempEdges = new List<Edge>();
            var cnt = outingPolygon.ptsIndex.Count;
            double maxLen = 0;
            for (int i = 0; i < cnt - 1; i++)
            {
                for (int j = i + 1; j < cnt; j++)
                {
                    var ptn1 = allPoints[outingPolygon.ptsIndex[i]];
                    var ptn2 = allPoints[outingPolygon.ptsIndex[j]];
                    var edgeLen = GetLength(ptn1.Point, ptn2.Point);
                    var edge = new Edge(ref edgeIndex, parentId, outingPolygon.ptsIndex[i], outingPolygon.ptsIndex[j], edgeLen);

                    if (edgeLen > maxLen)
                    {
                        maxLen = edgeLen-0.1;
                    }
                    tempEdges.Add(edge);
                }
            }
            foreach(var edge in tempEdges)
            {
                if(edge.Len< maxLen)
                {
                    edge.Id = edgeIndex;
                    edges.Add(edge);
                    edgeIndex++;
                }
            }
            return edges;
        }

        public double GetLength(PointVector pt1, PointVector pt2)
        {
            return Math.Abs(pt1.X-pt2.X) + Math.Abs(pt1.Y-pt2.Y) + Math.Abs(pt1.Z-pt2.Z);
        }

        public void GetTrianglesAndEdges(List<FaceTriangle> triangles, List<PointNormal> allPoints, ref int triangleIndex, ref int edgeIndex,
            UniComponent uniComponent, ref int ptIndex)
        {
            bool firstTriangles = true;
            foreach (var triangle in triangles)
            {
                var outingPolygon = new OutingPolygon(triangle, allPoints, ref triangleIndex, uniComponent, ref ptIndex, Points, firstTriangles);
                var edges = GetEdges(outingPolygon, allPoints, ref edgeIndex, uniComponent.unique_id);
                OutingPolygons.Add(outingPolygon);
                Edges.AddRange(edges);
                if (firstTriangles) firstTriangles = false;
            }
        }
    }
}
