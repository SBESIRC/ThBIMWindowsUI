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

        public List<PointNormal> allPoints = new List<PointNormal>();

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
                GetIfcFile(ifcStore, bimProject);
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
                AddIfcFile(ifcStore, bimProject);
            }
        }

        public void GetIfcFile(IfcStore ifcStore, THBimProject bimProject)
        {
            int ptIndex = 0;//点索引
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            allPoints.AddRange(bimProject.AllGeoPointNormals(true));

            var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();
            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    var floorPara = GetIfcStoreyPara(ifcStorey);
                    var buildingStorey = new Buildingstorey(ifcStorey, floorPara);
                    buildingStorey.element_index_s.Add(uniComponentIndex);
                    foreach (var spatialStructure in ifcStorey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;

                        foreach (var item in elements)
                        {
                            var type = item.ToString().Split('.').Last();
                            bool isVirtualElement = false;
                            if (type.Contains("IfcVirtualElement"))
                            {
                                type = "IfcSlab";
                                isVirtualElement = true;
                            }

                            var component = new Component(type, componentIndex);

                            if (!Components.ContainsKey(type))
                            {
                                Components.Add(type, component);
                                componentIndex++;
                            }
                            var uid = item.EntityLabel.ToString();
                            var material = THBimMaterial.GetTHBimEntityMaterial(type, true);

                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey, Components[type]);

                            GetProfileName(item, uniComponent);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                            }

                            uniComponent.edge_ind_e = edgeIndex - 1;
                            uniComponent.tri_ind_e = triangleIndex - 1;
                            uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                            if (isVirtualElement)
                            {
                                uniComponent.depth = 10;
                            }
                            else
                            {
                                uniComponent.depth = uniComponent.z_r - uniComponent.z_l;
                            }

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
            var typeName2IFCTypeName = new Dictionary<string, string>();
            typeName2IFCTypeName.Add("THBimWall", "IfcWall");
            typeName2IFCTypeName.Add("THBimSlab", "IfcSlab");
            typeName2IFCTypeName.Add("THBimBeam", "IfcBeam");
            typeName2IFCTypeName.Add("THBimWindow", "IfcWindow");
            typeName2IFCTypeName.Add("THBimColumn", "IfcColumn");
            typeName2IFCTypeName.Add("THBimRailing", "IfcRailing");
            typeName2IFCTypeName.Add("THBimDoor", "IfcDoor");


            int ptIndex = 0;//点索引
            int stdFloorIndex = 0;//标准层索引
            var floorStdDic = new Dictionary<string, int>();//楼层对应的标准层号
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            var allPoints = bimProject.AllGeoPointNormals(true);

            var storeys = bimProject.ProjectSite.SiteBuildings.Values.First().BuildingStoreys.Values;
            foreach(var storey in storeys)
            {
                int floorNum = Convert.ToInt32(storey.Name.Split('F').First());

                if (storey.MemoryStoreyId=="")
                {
                    var buildingStorey = new Buildingstorey(storey, floorNum, stdFloorIndex);
                    floorStdDic.Add(storey.Uid, stdFloorIndex);
                    stdFloorIndex++;
                    buildingStorey.element_index_s.Add(uniComponentIndex);

                    foreach (var relation in storey.FloorEntityRelations.Values)
                    {
                        var uid = relation.Uid;

                        var type = bimProject.PrjAllEntitys[uid].FriendlyTypeName;

                        if(!typeName2IFCTypeName.ContainsKey(type))
                        {
                            continue;
                        }
                        var ifcType = typeName2IFCTypeName[type];
                        if (!bimProject.PrjAllEntitys.ContainsKey(uid)) continue;
                        
                        var component = new Component(ifcType, componentIndex);
                        if (!Components.ContainsKey(ifcType))
                        {
                            Components.Add(ifcType, component);
                            componentIndex++;
                        }
                        var material = THBimMaterial.GetTHBimEntityMaterial(ifcType, true);
                        var uniComponent = new UniComponent(bimProject.PrjAllEntitys[uid], material, ref uniComponentIndex, buildingStorey, Components[ifcType]);
                        uniComponent.edge_ind_s = edgeIndex;
                        uniComponent.tri_ind_s = triangleIndex;
                        if (allGeoModels.ContainsKey(uid))
                        {
                            var triangles = allGeoModels[relation.RelationElementUid].FaceTriangles;
                            GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                        }
                        uniComponent.edge_ind_e = edgeIndex - 1;
                        uniComponent.tri_ind_e = triangleIndex - 1;
                        uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                        uniComponent.depth = uniComponent.z_r - uniComponent.z_l;

                        UniComponents.Add(uniComponent);

                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(buildingStorey);
                }
            }
            foreach (var storey in storeys)
            {
                int floorNum = Convert.ToInt32(storey.Name.Split('F').First());

                if (storey.MemoryStoreyId != "")
                {
                    var buildingStorey = new Buildingstorey(storey, floorNum, floorStdDic[storey.MemoryStoreyId]);
                    buildingStorey.element_index_s.Add(uniComponentIndex);

                    foreach (var relation in storey.FloorEntityRelations.Values)
                    {
                        var uid = relation.RelationElementUid;
                        if (!bimProject.PrjAllEntitys.ContainsKey(uid)) continue;
                        var type = bimProject.PrjAllEntitys[uid].FriendlyTypeName;
                        if (!typeName2IFCTypeName.ContainsKey(type))
                        {
                            continue;
                        }
                        var ifcType = typeName2IFCTypeName[type];
                        var material = THBimMaterial.GetTHBimEntityMaterial(ifcType, true);
                        var uniComponent = new UniComponent(bimProject.PrjAllEntitys[uid], material, ref uniComponentIndex, buildingStorey, Components[ifcType]);
                        uniComponent.edge_ind_s = edgeIndex;
                        uniComponent.tri_ind_s = triangleIndex;
                        if (allGeoModels.ContainsKey(uid))
                        {
                            var triangles = allGeoModels[relation.Uid].FaceTriangles;
                            GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                        }
                        uniComponent.edge_ind_e = edgeIndex - 1;
                        uniComponent.tri_ind_e = triangleIndex - 1;
                        uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                        uniComponent.depth = uniComponent.z_r - uniComponent.z_l;
                        UniComponents.Add(uniComponent);
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(buildingStorey);
                }
            }
        }

        public void AddIfcFile(IfcStore ifcStore, THBimProject bimProject)
        {
            int ptIndex = Points.Count;//点索引
            int componentIndex = Components.Count;//属性索引(门、窗等)
            int edgeIndex = Edges.Count;//边索引
            int triangleIndex = OutingPolygons.Count;//三角面片索引
            int uniComponentIndex = UniComponents.Count;//物体索引

            var allGeoModels = bimProject.AllGeoModels();
            allPoints.AddRange(bimProject.AllGeoPointNormals(true));
            var ifcProject = ifcStore.Instances.FirstOrDefault<Xbim.Ifc4.Interfaces.IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();

            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    var item1 = ifcStorey.ContainsElements.First().RelatedElements.First();
                    var item_Z = ((Xbim.Ifc2x3.GeometryResource.IfcPlacement)
                        ((Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement)
                        item1.ObjectPlacement).RelativePlacement).Location.Z;
                    var floorNo = GetFloorNo(item_Z);
                    if (floorNo < 0) continue;
                    foreach (var spatialStructure in ifcStorey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        Buildingstoreys[floorNo].element_index_s.Add(uniComponentIndex);
                        foreach (var item in elements)
                        {
                            var type = item.ToString().Split('.').Last();
                            bool isVirtualElement = false;
                            if (type.Contains("IfcVirtualElement"))
                            {
                                type = "IfcSlab";
                                isVirtualElement = true;
                            }
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
                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, Buildingstoreys[floorNo], Components[type]);
                            GetProfileName(item, uniComponent);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);

                                uniComponent.edge_ind_e = edgeIndex - 1;
                                uniComponent.tri_ind_e = triangleIndex - 1;
                                uniComponent.bg = uniComponent.z_r - Buildingstoreys[floorNo].elevation;
                                if (isVirtualElement)
                                {
                                    uniComponent.depth = 10;
                                }
                                else
                                {
                                    uniComponent.depth = uniComponent.z_r - uniComponent.z_l;
                                }
                                UniComponents.Add(uniComponent);
                            }
                            Buildingstoreys[floorNo].element_index_e.Add(uniComponentIndex - 1);
                        }
                    }
                }
            }
        }

        public int GetFloorNo(double itemz)
        {
            foreach(var storey in Buildingstoreys)
            {
                var buttom = storey.bottom_elevation;
                var top = storey.top_elevation;
                if(itemz <= top && itemz>=buttom)
                {
                    return storey.floorNo;
                }
            }
            return -100;
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
                storey.WriteToFile(writer);
                index++;
            }
            writer.Close();
        }

        public FloorPara GetIfcStoreyPara(IIfcBuildingStorey ifcStorey)
        {
            int floorNum = -1, stdFlrNum = -1;
            double height = -1;
            if(ifcStorey.Model.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                var storey = ifcStorey as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                foreach (var item in storey.PropertySets)
                {
                    if (item.PropertySetDefinitions == null) continue;
                    foreach (var prop in item.PropertySetDefinitions)
                    {
                        if (!(prop is Xbim.Ifc2x3.Interfaces.IIfcPropertySet)) continue;
                        var propertySet = prop as Xbim.Ifc2x3.Interfaces.IIfcPropertySet;
                        foreach (var realProp in propertySet.HasProperties)
                        {
                            var name = realProp.Name;
                            if (name == "FloorNo")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    int.TryParse(propValue.NominalValue.ToString(), out floorNum);
                                }
                            }
                            if (name == "StdFlrNo")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    int.TryParse(propValue.NominalValue.ToString(), out stdFlrNum);
                                }
                            }
                            if (name == "Height")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    double.TryParse(propValue.NominalValue.ToString(), out height);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                var storey = ifcStorey as Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
                foreach (var item in storey.PropertySets)
                {
                    if (item.PropertySetDefinitions == null) continue;
                    foreach (var prop in item.PropertySetDefinitions)
                    {
                        if (!(prop is Xbim.Ifc4.Interfaces.IIfcPropertySet)) continue;
                        var propertySet = prop as Xbim.Ifc4.Interfaces.IIfcPropertySet;
                        foreach (var realProp in propertySet.HasProperties)
                        {
                            var name = realProp.Name;
                            if (name == "FloorNo")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    int.TryParse(propValue.NominalValue.ToString(), out floorNum);
                                }
                            }
                            if (name == "StdFlrNo")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    int.TryParse(propValue.NominalValue.ToString(), out stdFlrNum);
                                }
                            }
                            if (name == "Height")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    double.TryParse(propValue.NominalValue.ToString(), out height);
                                }
                            }
                        }
                    }
                }
            }
            
            return new FloorPara(floorNum - 1, stdFlrNum - 1, height);
        }
     
        public void GetProfileName(IIfcProduct ifcEntity, UniComponent uniComponent)
        {
            var profileName = "";
            if (ifcEntity.Model.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                var ifcProduct = ifcEntity as Xbim.Ifc2x3.Kernel.IfcProduct;
                var item = ifcProduct.Representation.Representations.First().Items[0];
                if (item.GetType().Name == "IfcMappedItem")
                {
                    var source = (item as Xbim.Ifc2x3.GeometryResource.IfcMappedItem)
                        .MappingSource.MappedRepresentation.Items[0];
                    profileName = ((Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid)source).SweptArea.ProfileName.ToString();
                }
                else
                {
                    var solid = item as Xbim.Ifc2x3.GeometricModelResource.IfcExtrudedAreaSolid;
                    if (solid is null)
                    {
                        var rst = item as Xbim.Ifc2x3.GeometricModelResource.IfcBooleanResult;
                        if (rst is null)
                        {
                            return;
                        }
                        var solid2 = rst.FirstOperand;
                        profileName = ((Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid)solid2).SweptArea.ProfileName.ToString();
                    }
                    else
                    {
                        profileName = solid.SweptArea.ProfileName.ToString();
                    }
                }
            }
            else
            {
                var ifcProduct = ifcEntity as Xbim.Ifc4.Kernel.IfcProduct;
                if(!ifcProduct.Name.ToString().Contains("Beam"))
                {
                    return;
                }
                var item = ifcProduct.Representation.Representations.First().Items[0];
                var solid = item as Xbim.Ifc4.GeometricModelResource.IfcExtrudedAreaSolid;
                if (solid is null)
                {
                    var rst = item as Xbim.Ifc4.GeometricModelResource.IfcBooleanResult;
                    if (rst is null)
                    {
                        return;
                    }
                    var solid2 = rst.FirstOperand as Xbim.Ifc4.GeometricModelResource.IfcSweptAreaSolid;
                    profileName = solid2.SweptArea.ProfileName.ToString();
                }
                else
                {
                    profileName = solid.SweptArea.ProfileName.ToString();
                }
            }

            if (profileName.Contains("_") && profileName.Contains("*"))
            {
                string[] xyLen = profileName.Split('_')[1].Split('*');
                uniComponent.x_len = Convert.ToDouble(xyLen[0]);
                uniComponent.y_len = Convert.ToDouble(xyLen[1]);
            }
            else if(profileName.StartsWith("21"))
            {
                uniComponent.description = profileName;
            }

        }
       
        public List<Edge> GetEdgesByDir(OutingPolygon outingPolygon, List<PointNormal> allPoints, ref int edgeIndex, int parentId)
        {
            var edges = new List<Edge>();
            var cnt = outingPolygon.ptsIndex.Count;
            for (int i = 0; i < cnt - 1; i++)
            {
                for (int j = i + 1; j < cnt; j++)
                {
                    var ptn1 = allPoints[outingPolygon.ptsIndex[i]];
                    var ptn2 = allPoints[outingPolygon.ptsIndex[j]];
                    var edgeLen = GetLength(ptn1.Point, ptn2.Point);
                    var edge = new Edge(ref edgeIndex, parentId, outingPolygon.ptsIndex[i], outingPolygon.ptsIndex[j], edgeLen);

                    edges.Add(edge);
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
            var allEdges = new List<Edge>();
            foreach (var triangle in triangles)
            {
                var outingPolygon = new OutingPolygon(triangle, allPoints, ref triangleIndex, uniComponent, ref ptIndex, Points, firstTriangles);
                var edges = GetEdgesByDir(outingPolygon, allPoints, ref edgeIndex, uniComponent.unique_id);
                OutingPolygons.Add(outingPolygon);
                allEdges.AddRange(edges);
                if (firstTriangles) firstTriangles = false;
            }
            if (uniComponent.name.Contains("Beam"))
            {
                var edgeLenDic = new Dictionary<Vec3, double>();
                var edgeDic = new Dictionary<Vec3, List<Edge>>();
                int maxCnt = 0;
                foreach (var edge in allEdges)
                {
                    var pt1 = Points[edge.ptsIndex[0]];
                    var pt2 = Points[edge.ptsIndex[1]];
                    var vec3 = pt1.Dir(pt2);

                    bool added = false;
                    foreach(var vec in edgeDic.Keys)
                    {
                        if(vec.Equals(vec3))
                        {
                            edgeDic[vec].Add(edge);
                            if (edgeDic[vec].Count > maxCnt) maxCnt = edgeDic[vec].Count;
                            added = true;
                            break;
                        }
                    }
                    if (!added)
                    {
                        edgeLenDic.Add(vec3, vec3.Norm());
                        edgeDic.Add(vec3, new List<Edge>() { edge });
                    }
                }
                foreach (var key in edgeDic.Keys)
                {
                    if (edgeDic[key].Count == maxCnt)
                    {
                        foreach (var edge in edgeDic[key])
                        {
                            edge.Id = edgeIndex++;
                            Edges.Add(edge);
                        }
                    }
                }
            }
            else
            {
                foreach (var edge in allEdges)
                {
                    edge.Id = edgeIndex++;
                    Edges.Add(edge);
                }
            }
        }
    }
}
