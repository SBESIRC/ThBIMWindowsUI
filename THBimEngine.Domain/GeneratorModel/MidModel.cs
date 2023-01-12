using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Ifc;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace THBimEngine.Domain.GeneratorModel
{
    public class MidModel
    {
        public List<Vec3> Points;
        public Dictionary<int, Buildingstorey> Buildingstoreys;
        public Dictionary<string, Component> Components;
        public List<Edge> Edges;
        public List<OutingPolygon> OutingPolygons;
        public List<UniComponent> UniComponents;

        public List<PointNormal> allPoints = new List<PointNormal>();

        public MidModel()
        {
            Points = new List<Vec3>();
            Buildingstoreys = new Dictionary<int, Buildingstorey>();
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
                AddIfcFileByItem(ifcStore, bimProject);
            }
        }

        public void GetIfcFile(IfcStore ifcStore, THBimProject bimProject)
        {
            GetIfcFile(ifcStore, bimProject.AllGeoModels(), bimProject.AllGeoPointNormals());
        }

        public void GetIfcFile(IfcStore ifcStore, Dictionary<string, GeometryMeshModel> allGeoModels, List<PointNormal> allGeoPointNormals)
        {
            int ptIndex = 0;//点索引
            int componentIndex = 0;//属性索引(门、窗等)
            int edgeIndex = 0;//边索引
            int triangleIndex = 0;//三角面片索引
            int uniComponentIndex = 0;//物体索引

            var uniCompDepthDic = new Dictionary<string, double>();//“折板”构件名：标高
            int offsetIndex = allPoints.Count;
            foreach (var pt in allGeoPointNormals)
            {
                allPoints.Add(pt.GetRealData());
            }

            var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();

            var holeDic = new Dictionary<int, List<double>>();
            var holeDepthDic = new Dictionary<string, double>();
            var holeIdDic = new Dictionary<int, string>();
            var holeFloorDic = new Dictionary<int, int>();

            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    var floorPara = GetIfcStoreyPara(ifcStorey);
                    if (floorPara.Num == -100) continue;
                    var buildingStorey = new Buildingstorey(ifcStorey, floorPara, ifcStore.FileName);
                    buildingStorey.element_index_s.Add(uniComponentIndex);
                    foreach (var spatialStructure in ifcStorey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;

                        foreach (var item in elements)
                        {
                            if (item.Name.ToString().Contains("CantiSlab")) continue;

                            var description = GetDescription(item);
                            if (description == "S_BEAM_梯梁" || description == "S_COLU_梯柱")
                                continue;
                            if (uniCompDepthDic.ContainsKey(item.Name))
                            {
                                description = "折板";
                            }
                            var type = item.ToString().Split('.').Last();
                            if (type.Contains("IfcOpeningElement"))
                            {
                                try
                                {
                                    if (item.Model.SchemaVersion.ToString() == "Ifc4")
                                    {
                                        if (!item.Name.ToString().Contains("Wall_Hole"))
                                            continue;
                                        holeIdDic.Add(uniComponentIndex, item.GlobalId);
                                        holeFloorDic.Add(uniComponentIndex, floorPara.Num);
                                    }
                                    else
                                    {
                                        if (!((Xbim.Ifc2x3.Kernel.IfcRoot)item).FriendlyName.Contains("Wall_Hole"))
                                            continue;
                                        holeIdDic.Add(uniComponentIndex, item.GlobalId);
                                        holeFloorDic.Add(uniComponentIndex, floorPara.Num);
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
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
                            var materialType = "";
                            try
                            {
                                materialType = ((Xbim.Ifc2x3.MaterialResource.IfcMaterialLayerSetUsage)((Xbim.Ifc2x3.Kernel.IfcObjectDefinition)item).Material)?.ForLayerSet.LayerSetName.Value;
                            }
                            catch { }

                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, buildingStorey, Components[type], materialType, description);
                            uniComponent.description = item.Description;
                            GetProfileName(item, uniComponent);
                            if (item is Xbim.Ifc2x3.SharedBldgElements.IfcWall ifcwall)
                            {
                                if (ifcwall.HasOpenings.Count() > 0)
                                {
                                    holeDepthDic.Add(ifcwall.Openings.FirstOrDefault().GlobalId, uniComponent.y_len);
                                }
                            }
                            if (item is Xbim.Ifc4.SharedBldgElements.IfcWall ifcwall2)
                            {
                                if (ifcwall2.HasOpenings.Count() > 0)
                                {
                                    holeDepthDic.Add(ifcwall2.Openings.FirstOrDefault().GlobalId, uniComponent.y_len);
                                }
                            }

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, offsetIndex, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                            }

                            uniComponent.edge_ind_e = edgeIndex - 1;
                            uniComponent.tri_ind_e = triangleIndex - 1;
                            uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                            if (isVirtualElement)
                            {
                                uniComponent.depth = 10;
                            }
                            else if (description == "折板")
                            {
                                if (uniCompDepthDic.ContainsKey(item.Name))
                                {
                                    uniComponent.depth = uniCompDepthDic[item.Name];
                                }
                                else
                                {
                                    for (int i = uniComponent.edge_ind_s; i <= uniComponent.edge_ind_e; i++)
                                    {
                                        var edge = Edges[i];
                                        var pt1 = Points[edge.ptsIndex[0]];
                                        var pt2 = Points[edge.ptsIndex[1]];
                                        if (Math.Abs(pt1.x - pt2.x) < 1 && Math.Abs(pt1.y - pt2.y) < 1)
                                        {
                                            uniComponent.depth = Math.Abs(pt1.z - pt2.z);
                                            uniCompDepthDic.Add(item.Name + "_cloned", uniComponent.depth);
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                uniComponent.depth = uniComponent.z_r - uniComponent.z_l;
                            }

                            UniComponents.Add(uniComponent);
                            if (type.Contains("IfcOpeningElement"))
                            {
                                holeDic.Add(uniComponent.unique_id, new List<double>()
                                { uniComponent.x_l+uniComponent.x_r , uniComponent.y_l+uniComponent.y_r, uniComponent.z_l, uniComponent.z_r});
                            }
                        }
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(floorPara.Num, buildingStorey);
                }
                foreach (var i in holeDic.Keys)
                {
                    try
                    {
                        var storey = Buildingstoreys[UniComponents[i].floor_num];
                        UniComponents[i].properties.Add("Length", UniComponents[i].x_len.ToString());
                        UniComponents[i].properties.Add("Width", holeDepthDic[holeIdDic[i]].ToString());
                        UniComponents[i].properties.Add("Height", UniComponents[i].y_len.ToString());

                        foreach (var j in holeDic.Keys)
                        {
                            if (j <= i) continue;
                            var xDiff = Math.Abs(holeDic[i][0] - holeDic[j][0]);
                            var yDiff = Math.Abs(holeDic[i][1] - holeDic[j][1]);
                            var holeDepth = holeDic[j][2] - holeDic[i][3];
                            if (xDiff < 100 && yDiff < 100 && holeDepth > 0 && holeDepth < storey.top_elevation - storey.bottom_elevation && holeFloorDic[j] - holeFloorDic[i] == 1)
                            {
                                var storeyj = Buildingstoreys[UniComponents[j].floor_num];
                                UniComponents[i].properties.Add("LLHeight", (holeDic[j][2] - holeDic[i][3]).ToString());
                                UniComponents[i].properties.Add("LLElevation", (UniComponents[j].z_l - storeyj.bottom_elevation).ToString());
                                break;
                            }
                        }
                        if (!UniComponents[i].properties.ContainsKey("LLHeight"))
                        {
                            UniComponents[i].properties.Add("LLHeight", (storey.top_elevation - UniComponents[i].z_r).ToString());
                            UniComponents[i].properties.Add("LLElevation", "");
                        }
                    }
                    catch (Exception ex)
                    {
                        ;
                    }

                }
            }
        }

        public void AddIfcFile(IfcStore ifcStore, Dictionary<string, GeometryMeshModel> allGeoModels, List<PointNormal> allGeoPointNormals)
        {
            int ptIndex = Points.Count;
            int componentIndex = Components.Count;
            int edgeIndex = Edges.Count;
            int triangleIndex = OutingPolygons.Count;
            int uniComponentIndex = UniComponents.Count;

            int offsetIndex = allPoints.Count;
            foreach (var pt in allGeoPointNormals)
            {
                allPoints.Add(pt.GetRealData());
            }

            var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();
            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    foreach (var spatialStructure in ifcStorey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        foreach (var item in elements)
                        {
                            var type = item.ToString().Split('.').Last();

                            //过滤掉除了梁以外的构件
                            if (!type.Contains("IfcBeam"))
                                continue;
                            else
                                type = "IfcWall";
                            var materialType = "";
                            try
                            {
                                if (item.Material != null)
                                {
                                    materialType = ((Xbim.Ifc2x3.MaterialResource.IfcMaterialLayerSetUsage)((Xbim.Ifc2x3.Kernel.IfcObjectDefinition)item).Material).ForLayerSet.LayerSetName.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                ;
                            }

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


                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, Components[type], materialType, "PCWall");
                            GetProfileName(item, uniComponent);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, offsetIndex, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                                var minFloorNo = GetMinFloorNo(uniComponent.z_l);
                                var maxFloorNo = GetMinFloorNo(uniComponent.z_r);
                                if (minFloorNo == maxFloorNo)
                                {
                                    if (Buildingstoreys.ContainsKey(minFloorNo))
                                    {
                                        Buildingstoreys[minFloorNo].element_index_s.Add(uniComponentIndex - 1);
                                        Buildingstoreys[minFloorNo].element_index_e.Add(uniComponentIndex - 1);
                                    }
                                    else continue;
                                }
                                else
                                {
                                    for (int i = minFloorNo; i <= maxFloorNo; i++)
                                    {
                                        if (Buildingstoreys.ContainsKey(i))
                                        {
                                            Buildingstoreys[i].element_index_s.Add(uniComponentIndex - 1);
                                            Buildingstoreys[i].element_index_e.Add(uniComponentIndex - 1);
                                        }
                                    }
                                }

                                uniComponent.edge_ind_e = edgeIndex - 1;
                                uniComponent.tri_ind_e = triangleIndex - 1;
                                uniComponent.bg = uniComponent.z_r - Buildingstoreys[minFloorNo].elevation;
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
                    }
                }
            }
        }

        private string GetDescription(IIfcProduct ifcEntity)
        {
            var description = "";
            var ifcRoot = ifcEntity as Xbim.Ifc2x3.Kernel.IfcRoot;
            if (ifcRoot != null)
            {
                description = ifcRoot?.Description;
            }
            return description;
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

            int ptIndex = 0;
            int stdFloorIndex = 0;
            var floorStdDic = new Dictionary<string, int>();
            int componentIndex = 0;
            int edgeIndex = 0;
            int triangleIndex = 0;
            int uniComponentIndex = 0;

            int offsetIndex = allPoints.Count;

            var allGeoModels = bimProject.AllGeoModels();
            foreach (var pt in bimProject.AllGeoPointNormals())
            {
                allPoints.Add(pt.GetRealData());
            }

            var storeys = bimProject.ProjectSite.SiteBuildings.Values.First().BuildingStoreys.Values;
            foreach (var storey in storeys)
            {
                int floorNum = Convert.ToInt32(storey.Name.Split('F').First());

                if (storey.MemoryStoreyId == "")
                {
                    var buildingStorey = new Buildingstorey(storey, floorNum, stdFloorIndex);
                    floorStdDic.Add(storey.Uid, stdFloorIndex);
                    stdFloorIndex++;
                    buildingStorey.element_index_s.Add(uniComponentIndex);

                    foreach (var relation in storey.FloorEntityRelations.Values)
                    {
                        var uid = relation.Uid;
                        var type = bimProject.PrjAllEntitys[uid].FriendlyTypeName;

                        if (!typeName2IFCTypeName.ContainsKey(type))
                        {
                            continue;
                        }
                        var ifcType = typeName2IFCTypeName[type];
                        if (!bimProject.PrjAllEntitys.ContainsKey(uid))
                            continue;

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
                            GetTrianglesAndEdges(triangles, allPoints, offsetIndex, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                        }
                        uniComponent.edge_ind_e = edgeIndex - 1;
                        uniComponent.tri_ind_e = triangleIndex - 1;
                        uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                        uniComponent.depth = uniComponent.z_r - uniComponent.z_l;

                        UniComponents.Add(uniComponent);
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(floorNum, buildingStorey);
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
                            continue;

                        var ifcType = typeName2IFCTypeName[type];
                        var material = THBimMaterial.GetTHBimEntityMaterial(ifcType, true);
                        var uniComponent = new UniComponent(bimProject.PrjAllEntitys[uid], material, ref uniComponentIndex, buildingStorey, Components[ifcType]);
                        uniComponent.edge_ind_s = edgeIndex;
                        uniComponent.tri_ind_s = triangleIndex;
                        if (allGeoModels.ContainsKey(uid))
                        {
                            var triangles = allGeoModels[relation.Uid].FaceTriangles;
                            GetTrianglesAndEdges(triangles, allPoints, offsetIndex, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex);
                        }
                        uniComponent.edge_ind_e = edgeIndex - 1;
                        uniComponent.tri_ind_e = triangleIndex - 1;
                        uniComponent.bg = uniComponent.z_r - buildingStorey.elevation;
                        uniComponent.depth = uniComponent.z_r - uniComponent.z_l;
                        UniComponents.Add(uniComponent);
                    }
                    buildingStorey.element_index_e.Add(uniComponentIndex - 1);
                    Buildingstoreys.Add(floorNum, buildingStorey);
                }
            }
        }

        public void AddIfcFileByItem(IfcStore ifcStore, THBimProject bimProject)
        {
            bool reverse = bimProject.ApplcationName == EApplcationName.SU;
            int ptIndex = Points.Count;
            int componentIndex = Components.Count;
            int edgeIndex = Edges.Count;
            int triangleIndex = OutingPolygons.Count;
            int uniComponentIndex = UniComponents.Count;
            int offsetIndex = allPoints.Count;
            var allGeoModels = bimProject.AllGeoModels();
            foreach (var pt in bimProject.AllGeoPointNormals())
            {
                var realPt = pt.GetRealData();
                allPoints.Add(realPt);
            }
            var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
            var site = ifcProject.Sites.First();
            var buildings = site.Buildings.ToList();

            foreach (var building in buildings)
            {
                foreach (var ifcStorey in building.BuildingStoreys)
                {
                    foreach (var spatialStructure in ifcStorey.ContainsElements)
                    {
                        var elements = spatialStructure.RelatedElements;
                        if (elements.Count == 0) continue;
                        foreach (var item in elements)
                        {
                            var type = item.ToString().Split('.').Last();
                            var materialType = "";
                            try
                            {
                                if (item.Material != null)
                                {
                                    materialType = ((Xbim.Ifc2x3.MaterialResource.IfcMaterialLayerSetUsage)((Xbim.Ifc2x3.Kernel.IfcObjectDefinition)item).Material).ForLayerSet.LayerSetName.Value;
                                }
                            }
                            catch (Exception ex)
                            {
                                ;
                            }

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

                            var uniComponent = new UniComponent(uid, material, ref uniComponentIndex, Components[type], materialType);
                            GetProfileName(item, uniComponent);

                            uniComponent.edge_ind_s = edgeIndex;
                            uniComponent.tri_ind_s = triangleIndex;
                            if (allGeoModels.ContainsKey(uid))
                            {
                                var triangles = allGeoModels[uid].FaceTriangles;
                                GetTrianglesAndEdges(triangles, allPoints, offsetIndex, ref triangleIndex, ref edgeIndex, uniComponent, ref ptIndex, reverse);
                                var minFloorNo = GetMinFloorNo(uniComponent.z_l);
                                var maxFloorNo = GetMinFloorNo(uniComponent.z_r);
                                if (minFloorNo == maxFloorNo)
                                {
                                    if (Buildingstoreys.ContainsKey(minFloorNo))
                                    {
                                        Buildingstoreys[minFloorNo].element_index_s.Add(uniComponentIndex - 1);
                                        Buildingstoreys[minFloorNo].element_index_e.Add(uniComponentIndex - 1);
                                    }
                                    else continue;
                                }
                                else
                                {
                                    for (int i = minFloorNo; i <= maxFloorNo; i++)
                                    {
                                        if (Buildingstoreys.ContainsKey(i))
                                        {
                                            Buildingstoreys[i].element_index_s.Add(uniComponentIndex - 1);
                                            Buildingstoreys[i].element_index_e.Add(uniComponentIndex - 1);
                                        }
                                    }
                                }

                                uniComponent.edge_ind_e = edgeIndex - 1;
                                uniComponent.tri_ind_e = triangleIndex - 1;
                                uniComponent.bg = uniComponent.z_r - Buildingstoreys[minFloorNo].elevation;
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
                    }
                }
            }
        }

        public int GetMinFloorNo(double itemz)
        {
            foreach (var storey in Buildingstoreys.Values)
            {
                var buttom = storey.bottom_elevation;
                var top = storey.top_elevation;
                if (itemz <= top && itemz >= buttom)
                {
                    return storey.floorNo;
                }
            }
            return -100;
        }

        public void WriteMidFile(string ifcPath = null)
        {
            string fileName = Path.Combine(System.IO.Path.GetTempPath(), "BimEngineData.get");
            string storeyFileName = Path.Combine(System.IO.Path.GetTempPath(), "BimEngineData.storeys.txt");

            if (ifcPath != null)
            {
                fileName = Path.Combine(System.IO.Path.GetDirectoryName(ifcPath), "BimEngineData.get");
                storeyFileName = Path.Combine(System.IO.Path.GetDirectoryName(ifcPath), "BimEngineData.storeys.txt");
            }

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
            FileStream fs = new FileStream(storeyFileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
            foreach (var storey in Buildingstoreys.Values)
            {
                storey.WriteToFile(writer);
                storey.WriteToTxt(sw);
                index++;
            }
            sw.Flush();
            sw.Close();
            fs.Close();
            writer.Close();
        }

        public FloorPara GetIfcStoreyPara(IIfcBuildingStorey ifcStorey)
        {
            int floorNum = -100, stdFlrNum = -100;
            double height = -100;
            if (ifcStorey.Model.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
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
                            if (name == "FloorNo" || name == "名称")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    var val = propValue.NominalValue.ToString();
                                    if (val.Contains("B") && val.Contains("F"))
                                    {
                                        int.TryParse(val.Split('B').Last().Split('F').First(), out floorNum);
                                        floorNum *= -1;
                                    }
                                    else if (val.Contains("F"))
                                    {
                                        int.TryParse(val.Split('F').First(), out floorNum);
                                    }
                                    else
                                    {
                                        int.TryParse(val, out floorNum);
                                    }
                                }
                            }
                            if (name == "StdFlrNo")
                            {
                                if (realProp is IIfcPropertySingleValue propValue)
                                {
                                    int.TryParse(propValue.NominalValue.ToString(), out stdFlrNum);
                                }
                            }
                            if (name == "Height" || name == "层高")
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
            if (stdFlrNum < 0) stdFlrNum = floorNum;
            return new FloorPara(floorNum, stdFlrNum, height);
        }

        public void GetProfileName(IIfcProduct ifcEntity, UniComponent uniComponent)
        {
            try
            {
                var profileName = "";
                if (ifcEntity.Name.Value.ToString().Contains("SU") && ifcEntity.GetType().Name.Contains("Beam"))
                {
                    var x = (ifcEntity as Xbim.Ifc2x3.Kernel.IfcObject).IsDefinedByProperties;
                    var y = ((Xbim.Ifc2x3.Kernel.IfcPropertySet)x.First().RelatingPropertyDefinition).HasProperties.First();
                    var z = (string)(y as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value;
                    var val1 = z.Split(',')[1];
                    var val2 = z.Split(',').Last();
                    profileName = "Rec_" + val1 + "*" + val2;
                }
                else if (ifcEntity.Model.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
                {
                    var ifcProduct = ifcEntity as Xbim.Ifc2x3.Kernel.IfcProduct;
                    var item = ifcProduct.Representation.Representations.First().Items[0];
                    var type = item.GetType().Name;
                    if (type == "IfcMappedItem")
                    {
                        var mappedItem = (item as Xbim.Ifc2x3.GeometryResource.IfcMappedItem).MappingSource.MappedRepresentation.Items.FirstOrDefault();
                        profileName = GetProfileName(mappedItem, mappedItem.GetType().Name);
                    }
                    else
                    {
                        profileName = GetProfileName(item, type);
                    }
                }
                else
                {
                    var ifcProduct = ifcEntity as Xbim.Ifc4.Kernel.IfcProduct;
                    var item = ifcProduct.Representation.Representations.First().Items[0];
                    var type = item.GetType().Name;
                    if (type == "IfcMappedItem")
                    {
                        var mappedItem = (item as Xbim.Ifc4.GeometryResource.IfcMappedItem).MappingSource.MappedRepresentation.Items.FirstOrDefault();
                        profileName = GetProfileName(mappedItem, mappedItem.GetType().Name);
                    }
                    else
                    {
                        profileName = GetProfileName(item, type);
                    }
                }
                if (string.IsNullOrEmpty(profileName)) return;
                if (profileName.Contains("_") && profileName.Contains("*"))
                {
                    string[] xyLen = profileName.Split('_')[1].Split('*');
                    uniComponent.x_len = Convert.ToDouble(xyLen[0]);
                    uniComponent.y_len = Convert.ToDouble(xyLen[1]);
                }
                else if (profileName.StartsWith("21"))
                {
                    uniComponent.properties.Add("ProfileName", profileName);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private string GetProfileName(PersistEntity item, string type)
        {
            if (item.Model.SchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                if (type == "IfcRevolvedAreaSolid")
                {
                    return (item as Xbim.Ifc2x3.GeometricModelResource.IfcRevolvedAreaSolid).SweptArea.ProfileName;
                }
                else if (type == "IfcExtrudedAreaSolid")
                {
                    return (item as Xbim.Ifc2x3.GeometricModelResource.IfcExtrudedAreaSolid).SweptArea.ProfileName;

                }
                else if (type == "IfcBooleanResult")
                {
                    string profileName = "";
                    GetProfileName(item as Xbim.Ifc2x3.GeometricModelResource.IfcBooleanResult, ref profileName);
                    return profileName;
                }
            }
            else
            {
                if (type == "IfcRevolvedAreaSolid")
                {
                    return (item as Xbim.Ifc4.GeometricModelResource.IfcRevolvedAreaSolid).SweptArea.ProfileName;
                }
                else if (type == "IfcExtrudedAreaSolid")
                {
                    return (item as Xbim.Ifc4.GeometricModelResource.IfcExtrudedAreaSolid).SweptArea.ProfileName;

                }
                else if (type == "IfcBooleanResult"|| type == "IfcBooleanClippingResult")
                {
                    string profileName = "";
                    GetProfileName(item as Xbim.Ifc4.GeometricModelResource.IfcBooleanResult, ref profileName);
                    return profileName;
                }
            }
            return "";
        }

        private void GetProfileName(Xbim.Ifc4.GeometricModelResource.IfcBooleanResult item, ref string profileName)
        {
            var firstOperand = item.FirstOperand;
            if (firstOperand.GetType().Name == "IfcBooleanResult")
            {
                GetProfileName(firstOperand as Xbim.Ifc4.GeometricModelResource.IfcBooleanResult, ref profileName);
            }
            else
            {
                if(firstOperand.GetType().Name== "IfcBooleanClippingResult")
                {
                    GetProfileName(firstOperand as Xbim.Ifc4.GeometricModelResource.IfcBooleanClippingResult, ref profileName);
                }
                else
                {
                    profileName = (firstOperand as Xbim.Ifc4.GeometricModelResource.IfcSweptAreaSolid).SweptArea.ProfileName.ToString();
                }
            }
        }
        private void GetProfileName(Xbim.Ifc2x3.GeometricModelResource.IfcBooleanResult item, ref string profileName)
        {
            var firstOperand = item.FirstOperand;
            if (firstOperand.GetType().Name == "IfcBooleanResult")
            {
                GetProfileName(firstOperand as Xbim.Ifc2x3.GeometricModelResource.IfcBooleanResult, ref profileName);
            }
            else
            {
                profileName = (firstOperand as Xbim.Ifc2x3.GeometricModelResource.IfcSweptAreaSolid).SweptArea.ProfileName.ToString();
            }
        }

        public List<Edge> GetEdgesByDir(OutingPolygon outingPolygon, List<Vec3> allPoints, ref int edgeIndex, int parentId)
        {
            var edges = new List<Edge>();
            var cnt = outingPolygon.ptsIndex.Count;
            for (int i = 0; i < cnt - 1; i++)
            {
                for (int j = i + 1; j < cnt; j++)
                {
                    //var pt = allPoints[triangle.ptIndex[i] + offsetIndex].Point;
                    var ptn1 = allPoints[outingPolygon.ptsIndex[i]];
                    var ptn2 = allPoints[outingPolygon.ptsIndex[j]];
                    var edgeLen = GetLength(ptn1, ptn2);
                    var edge = new Edge(ref edgeIndex, parentId, outingPolygon.ptsIndex[i], outingPolygon.ptsIndex[j], edgeLen);

                    edges.Add(edge);
                }
            }
            return edges;
        }

        public double GetLength(Vec3 pt1, Vec3 pt2)
        {
            return Math.Sqrt(Math.Pow(pt1.x - pt2.x, 2) + Math.Pow(pt1.y - pt2.y, 2));
        }

        public void GetTrianglesAndEdges(List<FaceTriangle> triangles, List<PointNormal> allPoints, int offsetIndex, ref int triangleIndex, ref int edgeIndex,
            UniComponent uniComponent, ref int ptIndex, bool reverse = false)
        {
            bool firstTriangles = true;
            var allEdges = new List<Edge>();
            var pts = new List<PointVector>();
            foreach (var triangle in triangles)
            {
                var outingPolygon = new OutingPolygon(triangle, allPoints, offsetIndex, ref triangleIndex, uniComponent,
                    ref ptIndex, Points, firstTriangles, reverse);
                var edges = GetEdgesByDir(outingPolygon, Points, ref edgeIndex, uniComponent.unique_id);
                OutingPolygons.Add(outingPolygon);
                allEdges.AddRange(edges);
                if (firstTriangles) firstTriangles = false;
            }
            if (uniComponent.name.Contains("Beam"))
            {
                if (uniComponent.properties.ContainsKey("ProfileName"))
                {
                    var lengths = new List<int>();
                    //var xylen = Math.Max(uniComponent.x_r-uniComponent.x_l,uniComponent.y_r-uniComponent.y_l);
                    Dictionary<int, int> dic = new Dictionary<int, int>();
                    foreach (var edge in allEdges)
                    {
                        if (!lengths.Contains(Convert.ToInt32(edge.Len)))
                            lengths.Add(Convert.ToInt32(edge.Len));
                    }
                    lengths = lengths.OrderByDescending(l => l).ToList();

                    foreach (var edge in allEdges)
                    {
                        if (Math.Abs(edge.Len - lengths[1]) <= 1)
                        {
                            edge.Id = edgeIndex++;
                            Edges.Add(edge);
                        }
                    }
                    return;
                }
                var edgeLenDic = new Dictionary<Vec3, double>();
                var edgeDic = new Dictionary<Vec3, List<Edge>>();
                int maxCnt = 0;
                foreach (var edge in allEdges)
                {
                    var pt1 = Points[edge.ptsIndex[0]];
                    var pt2 = Points[edge.ptsIndex[1]];
                    var vec3 = pt1.Dir(pt2);

                    bool added = false;
                    foreach (var vec in edgeDic.Keys)
                    {
                        if (vec.Equals(vec3))
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
