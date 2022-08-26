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
        public Dictionary<string,Component> Components;
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
            int ptIndex = 0;//点索引
            int buildingIndex = 0;//建筑物索引
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            var allPoints = bimProject.AllGeoPointNormals();

            var ifcStore = bimProject.SourceProject as IfcStore;
            if (ifcStore != null)//处理ifc数据
            {
                var ifcProject = ifcStore.Instances.FirstOrDefault<Xbim.Ifc4.Interfaces.IIfcProject>();
                var site = ifcProject.Sites.First();
                var buildings = site.Buildings.ToList();
                foreach(var building in buildings)
                {
                    foreach (var ifcStorey in building.BuildingStoreys)
                    {

                        if (ifcStorey.GetType().FullName== "Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey")
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
                                    var material = THBimMaterial.GetTHBimEntityMaterial("Default", true);
                                    if (bimProject.PrjAllEntitys.ContainsKey(uid))
                                    {
                                        material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true);
                                    }
                                    var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey, component);
                                    

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
                        else
                        {
                            var storey = ifcStorey as Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
                            var buildingStorey = new Buildingstorey(storey, ref buildingIndex);
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
                                buildingStorey.element_index_s.Add(uniComponentIndex);
                                foreach (var item in elements)
                                {
                                    var uid = item.EntityLabel.ToString();
                                    var material = THBimMaterial.GetTHBimEntityMaterial("Default", true);
                                    if (bimProject.PrjAllEntitys.ContainsKey(uid))
                                        material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true);
                                    var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey,component);
                                    UniComponents.Add(uniComponent);

                                    uniComponent.edge_ind_s = edgeIndex;
                                    uniComponent.tri_ind_s = triangleIndex;
                                    if(allGeoModels.ContainsKey(uid))
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
                                }
                            }
                            buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                            Buildingstoreys.Add(buildingStorey);
                        }
                    }
                }
                return;
            }

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
                var buildingStorey = new Buildingstorey(storey,ref buildingIndex);
                buildingStorey.element_index_s.Add(uniComponentIndex);

                foreach(var relation in storey.FloorEntityRelations.Values)
                {
                    var uid = relation.RelationElementUid;
                    var material = THBimMaterial.GetTHBimEntityMaterial(bimProject.PrjAllEntitys[uid].FriendlyTypeName, true); 
                    var uniComponent = new UniComponent(relation, material,ref uniComponentIndex, buildingStorey);
                    UniComponents.Add(uniComponent);

                    uniComponent.edge_ind_s = edgeIndex;
                    uniComponent.tri_ind_s = triangleIndex;
                    var triangles = allGeoModels[relation.RelationElementUid].FaceTriangles;
                    GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                    uniComponent.edge_ind_e = edgeIndex - 1;
                    uniComponent.tri_ind_e = triangleIndex - 1;
                }
                buildingStorey.element_index_e.Add(uniComponentIndex -1);
                Buildingstoreys.Add(buildingStorey);
            }
        }

        public void WriteMidFile()
        {
            string fileName = Path.Combine(System.IO.Path.GetTempPath(), "BimEngineData.get");
            FileStream fileStream = new FileStream(fileName,FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
            BinaryWriter writer = new BinaryWriter(fileStream,Encoding.UTF8);
            int cnt = OutingPolygons.Count;
            writer.Write(cnt);
            foreach (var trangle in OutingPolygons)
            {
                trangle.WriteToFile(writer, Points);
            }
            //writer.Write("triaOK".ToCharArray());
            cnt = Edges.Count;
            writer.Write(cnt);
            foreach (var edge in Edges)
            {
                edge.WriteToFile(writer, Points);
            }
            //writer.Write("edgeOK".ToCharArray());
            cnt = Components.Count;
            writer.Write(cnt);
            foreach (var component in Components.Values)
            {
                component.WriteToFile(writer);
            }
            //writer.Write("compOK".ToCharArray());
            cnt = UniComponents.Count;
            writer.Write(cnt);
            foreach (var uniComponent in UniComponents)
            {
                uniComponent.WriteToFile(writer);
            }
            //writer.Write("unicOK".ToCharArray());
            cnt = Buildingstoreys.Count;
            writer.Write(cnt);
            int index = 0;
            foreach (var storey in Buildingstoreys)
            {
                if(index==12)
                {
                    ;
                }
                storey.WriteToFile(writer);
                index++;
            }
            //writer.Write("storOK".ToCharArray());
            writer.Close();
        }

        public double GetIfcStoreyHeight(Xbim.Ifc4.ProductExtension.IfcBuildingStorey storey)
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

        public List<Edge> GetEdges(OutingPolygon outingPolygon, ref int edgeIndex, int parentId)
        {
            var edges = new List<Edge>();
            var cnt = outingPolygon.ptsIndex.Count;
            for (int i =0;i<cnt-1;i++)
            {
                for(int j =i+1; j< cnt; j++)
                {
                    var edge = new Edge(ref edgeIndex, parentId, outingPolygon.ptsIndex[i], outingPolygon.ptsIndex[j]);
                    edges.Add(edge);
                }
            }
            return edges;
        }

        public void GetTrianglesAndEdges(List<FaceTriangle> triangles, List<PointNormal> allPoints, ref int triangleIndex, ref int edgeIndex,
            UniComponent uniComponent, ref int ptIndex)
        {
            foreach(var triangle in triangles)
            {
                var outingPolygon = new OutingPolygon(triangle, allPoints, ref triangleIndex, uniComponent, ref ptIndex, Points);
                var edges = GetEdges(outingPolygon, ref edgeIndex, uniComponent.unique_id);
                OutingPolygons.Add(outingPolygon);
                Edges.AddRange(edges);
            }
        }
    }
}
