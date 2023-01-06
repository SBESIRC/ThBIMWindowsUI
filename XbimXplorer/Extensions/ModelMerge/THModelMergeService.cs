using System;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.Application;
using THBimEngine.Domain;
using ThBIMServer.Ifc2x3;
using ThBIMServer.Ifc4;
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;

namespace XbimXplorer.Extensions.ModelMerge
{
    public class THModelMergeService
    {
        public IfcStore ModelMerge(string filePath1, string filePath2)
        {
            var model = IfcStore.Open(filePath1);
            var model2 = IfcStore.Open(filePath2);
            if (model.Instances.Count > model2.Instances.Count)
            {
                return ModelMerge(model, model2);
            }
            else
            {
                return ModelMerge(model2, model);
            }
        }

        /// <summary>
        /// IFC合模
        /// </summary>
        /// <param name="bigModel">95%ifc</param>
        /// <param name="smallModel">5%ifc</param>
        /// <returns></returns>
        public IfcStore ModelMerge(IfcStore model, IfcStore smallModel)
        {
            if (string.IsNullOrEmpty(model.FileName))
                return null;
            var bigModel = IfcStore.Open(model.FileName);

            //先做一个最简单的，两个Ifc2X3的IFC模型合并，后续再考虑版本问题
            var bigProject = bigModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            var smallProject = smallModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();

            var bigBuildings = bigProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
            var smallBuildings = smallProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault();
            var MaxStdFlrNo = 0;
            //处理95%
            List<Tuple<int, double, double>> StoreyDic = new List<Tuple<int, double, double>>();
            foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in bigBuildings.BuildingStoreys)
            {
                double Storey_Elevation = BuildingStorey.Elevation.Value;
                double Storey_Height = double.Parse(((BuildingStorey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                StoreyDic.Add((int.Parse(BuildingStorey.Name.ToString()), Storey_Elevation, Storey_Height).ToTuple());
                //获取标准层信息，因为不是所有的IFC都会符合我们约定俗成的"标准"，所以这里加上try...catch
                //即符合我们标准的我们获取，不符合的"跳过"
                try
                {
                    var property = BuildingStorey.Model.Instances.OfType<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(BuildingStorey));
                    if (property != null)
                    {
                        var stdFlrNoPrp = (property.RelatingPropertyDefinition as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties
                            .Where(o => o.Name.Equals("StdFlrNo")).FirstOrDefault() 
                            as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue;
                        var stdFlrNo = int.Parse(stdFlrNoPrp.NominalValue.ToString());
                        MaxStdFlrNo = Math.Max(stdFlrNo, MaxStdFlrNo);
                    }
                }
                catch
                {
                    // do not
                }
            }
            StoreyDic = StoreyDic.OrderBy(x => x.Item1).ToList();

            PropertyTranformDelegate semanticFilter = (property, parentObject) =>
            {
                return property.PropertyInfo.GetValue(parentObject, null);
            };
            //single map should be used for all insertions between two models
            var map = new XbimInstanceHandleMap(smallModel, bigModel);

            //处理5%
            foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in smallBuildings.BuildingStoreys)
            {
                var bigStorey = StoreyDic.FirstOrDefault(o => o.Item1.ToString() == BuildingStorey.Name);
                double storey_heigth = 0;
                double Storey_z = 0;
                if (bigStorey == null)
                {
                    Storey_z = ((BuildingStorey.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z;
                    var relatedElements = BuildingStorey.ContainsElements.SelectMany(o => o.RelatedElements).Where(o =>
                    o is Xbim.Ifc2x3.SharedBldgElements.IfcWall || o is Xbim.Ifc2x3.SharedBldgElements.IfcBeam || o is Xbim.Ifc2x3.SharedBldgElements.IfcSlab || o is Xbim.Ifc2x3.SharedBldgElements.IfcColumn || o is Xbim.Ifc2x3.SharedBldgElements.IfcWindow || o is Xbim.Ifc2x3.SharedBldgElements.IfcDoor);
                    if (relatedElements.Any())
                    {
                        //找到该楼层的所有构建，找到最低的Location.Z
                        var relatedElement_minz = relatedElements.Min(o => ((o.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as  Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z);
                        var relatedElement_maxz = relatedElements.Max(o => ((o.ObjectPlacement as Xbim.Ifc2x3.GeometricConstraintResource.IfcLocalPlacement).RelativePlacement as  Xbim.Ifc2x3.GeometryResource.IfcPlacement).Location.Z);
                        Storey_z += relatedElement_minz;
                        storey_heigth = relatedElement_maxz - relatedElement_minz;
                    }
                    bigStorey = StoreyDic.FirstOrDefault(o => Math.Abs(o.Item2 - Storey_z) <= 200);
                    if (bigStorey == null)
                    {
                        if (Math.Abs(Storey_z - (StoreyDic.Last().Item2 + StoreyDic.Last().Item3)) <= 200)
                        {
                            //楼层高度 = 最顶层的标高 + 最顶层的层高，说明这个是新的一层
                            var storeyNo = StoreyDic.Last().Item1 + 1;
                            StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                            bigStorey = StoreyDic.Last();
                        }
                        else if (Storey_z < StoreyDic.First().Item2)
                        {
                            var storeyNo = StoreyDic.First().Item1 - 1;
                            if (storeyNo == 0)
                            {
                                storeyNo--;
                            }
                            StoreyDic.Insert(0, (storeyNo, Storey_z, StoreyDic.First().Item2 - Storey_z).ToTuple());
                            bigStorey = StoreyDic.First();
                        }
                        else if (Storey_z > (StoreyDic.Last().Item2 + StoreyDic.Last().Item3))
                        {
                            var storeyNo = StoreyDic.Last().Item1 + 1;
                            StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                            bigStorey = StoreyDic.Last();
                        }
                        else
                        {
                            bigStorey = StoreyDic.FirstOrDefault(o => Storey_z - o.Item2 > -200);
                        }
                    }
                }
                var storeyName = bigStorey.Item1.ToString().Replace('-', 'B');
                var storey = bigBuildings.BuildingStoreys.FirstOrDefault(o => o.Name==storeyName) as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                if (storey == null)
                {
                    storey = BuildingStorey.CloneAndCreateNew(bigModel, bigBuildings, storeyName, Storey_z,  storey_heigth, ++MaxStdFlrNo);
                }
                var CreatElements = new List<Xbim.Ifc2x3.Kernel.IfcProduct>();
                foreach (var spatialStructure in BuildingStorey.ContainsElements)
                {
                    var elements = spatialStructure.RelatedElements;
                    using (var txn = bigModel.BeginTransaction("Insert copy"))
                    {
                        foreach (var element in elements)
                        {
                            var newElement = bigModel.InsertCopy(element, map, semanticFilter, false, false);
                            // add properties
                            var property = smallModel.Instances.OfType<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(element));
                            if (property != null)
                            {
                                var ifcRelDefinesByProperties = property.CloneAndCreateNew(bigModel);
                                ifcRelDefinesByProperties.RelatedObjects.Add(newElement);
                            }
                            CreatElements.Add(newElement);
                        }
                        txn.Commit();
                    }
                }
                using (var txn = bigModel.BeginTransaction("relContainEntitys2Storey"))
                {
                    //for ifc2x3
                    var relContainedIn = bigModel.Instances.New<Xbim.Ifc2x3.ProductExtension.IfcRelContainedInSpatialStructure>();
                    storey.ContainsElements.Append<Xbim.Ifc2x3.Interfaces.IIfcRelContainedInSpatialStructure>(relContainedIn);

                    relContainedIn.RelatingStructure = storey;
                    relContainedIn.RelatedElements.AddRange(CreatElements);
                    txn.Commit();
                }
            }

            //返回
            return bigModel;
        }

        public IfcStore ModelMerge(IfcStore model, ThSUProjectData suProject)
        {
            if(suProject == null || string.IsNullOrEmpty(model.FileName))
                return null;
            var bigModel = IfcStore.Open(model.FileName);
            if(bigModel.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc2X3)
            {
                //先做一个最简单的，两个Ifc2X3的IFC模型合并，后续再考虑版本问题
                var bigProject = bigModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
                //var smallProject = smallModel.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();

                var bigBuildings = bigProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc2x3.ProductExtension.IfcBuilding;
                //var smallBuildings = smallProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault();
                var MaxStdFlrNo = 0;
                //处理95%
                List<Tuple<int, double, double>> StoreyDic = new List<Tuple<int, double, double>>();
                foreach (Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey BuildingStorey in bigBuildings.BuildingStoreys)
                {
                    double Storey_Elevation = BuildingStorey.Elevation.Value;
                    double Storey_Height = double.Parse(((BuildingStorey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                    StoreyDic.Add((int.Parse(BuildingStorey.Name.ToString()), Storey_Elevation, Storey_Height).ToTuple());
                    //获取标准层信息，因为不是所有的IFC都会符合我们约定俗成的"标准"，所以这里加上try...catch
                    //即符合我们标准的我们获取，不符合的"跳过"
                    try
                    {
                        var property = BuildingStorey.Model.Instances.OfType<Xbim.Ifc2x3.Kernel.IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(BuildingStorey));
                        if (property != null)
                        {
                            var stdFlrNoPrp = (property.RelatingPropertyDefinition as Xbim.Ifc2x3.Kernel.IfcPropertySet).HasProperties
                                .Where(o => o.Name.Equals("StdFlrNo")).FirstOrDefault()
                                as Xbim.Ifc2x3.PropertyResource.IfcPropertySingleValue;
                            var stdFlrNo = int.Parse(stdFlrNoPrp.NominalValue.ToString());
                            MaxStdFlrNo = Math.Max(stdFlrNo, MaxStdFlrNo);
                        }
                    }
                    catch
                    {
                        // do not
                    }
                }
                StoreyDic = StoreyDic.OrderBy(x => x.Item1).ToList();

                //处理5%
                var definitions = suProject.Definitions;
                var storeys = new List<IfcBuildingStorey>();
                foreach (var BuildingStorey in suProject.Building.Storeys)
                {
                    var bigStorey = StoreyDic.FirstOrDefault(o => o.Item1 == BuildingStorey.Number);
                    double Storey_z = 0;
                    if (bigStorey == null)
                    {
                        Storey_z = BuildingStorey.Elevation;
                        bigStorey = StoreyDic.FirstOrDefault(o => Math.Abs(o.Item2 - Storey_z) <= 200);
                        if (bigStorey == null)
                        {
                            if (Math.Abs(Storey_z - (StoreyDic.Last().Item2 + StoreyDic.Last().Item3)) <= 200)
                            {
                                //楼层高度 = 最顶层的标高 + 最顶层的层高，说明这个是新的一层
                                var storeyNo = StoreyDic.Last().Item1 + 1;
                                StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                                bigStorey = StoreyDic.Last();
                            }
                            else if (Storey_z < StoreyDic.First().Item2)
                            {
                                var storeyNo = StoreyDic.First().Item1 - 1;
                                if (storeyNo == 0)
                                {
                                    storeyNo--;
                                }
                                StoreyDic.Insert(0, (storeyNo, Storey_z, StoreyDic.First().Item2 - Storey_z).ToTuple());
                                bigStorey = StoreyDic.First();
                            }
                            else if (Storey_z > (StoreyDic.Last().Item2 + StoreyDic.Last().Item3))
                            {
                                var storeyNo = StoreyDic.Last().Item1 + 1;
                                StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                                bigStorey = StoreyDic.Last();
                            }
                            else
                            {
                                bigStorey = StoreyDic.FirstOrDefault(o => Storey_z - o.Item2 > -200);
                            }
                        }
                    }
                    var storeyName = bigStorey.Item1.ToString().Replace('-', 'B');
                    var storey = bigBuildings.BuildingStoreys.FirstOrDefault(o => o.Name==storeyName) as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                    if (storey == null)
                    {
                        storey = ThProtoBuf2IFC2x3Factory.CreateStorey(bigModel, bigBuildings, BuildingStorey);
                        storeys.Add(storey);
                    }
                    var suElements = new List<IfcBuildingElement>();
                    foreach (var element in BuildingStorey.Buildings)
                    {
                        var def = definitions[element.Component.DefinitionIndex];
                        IfcBuildingElement ifcBuildingElement;
                        ifcBuildingElement = ThProtoBuf2IFC2x3Factory.CreatedSUElement(bigModel, def, element.Component);
                        suElements.Add(ifcBuildingElement);
                    }
                    using (var txn = bigModel.BeginTransaction("relContainEntitys2Storey"))
                    {
                        //for ifc2x3
                        var relContainedIn = bigModel.Instances.New<Xbim.Ifc2x3.ProductExtension.IfcRelContainedInSpatialStructure>();
                        storey.ContainsElements.Append<Xbim.Ifc2x3.Interfaces.IIfcRelContainedInSpatialStructure>(relContainedIn);

                        relContainedIn.RelatingStructure = storey;
                        relContainedIn.RelatedElements.AddRange(suElements);
                        txn.Commit();
                    }
                }
                // IfcRelAggregates 关系
                ThProtoBuf2IFC2x3RelAggregatesFactory.Create(bigModel, bigBuildings, storeys);
                //返回
                return bigModel;
            }
            else if(bigModel.IfcSchemaVersion == Xbim.Common.Step21.IfcSchemaVersion.Ifc4)
            {
                var bigProject = bigModel.Instances.FirstOrDefault<Xbim.Ifc4.Kernel.IfcProject>();

                var bigBuildings = bigProject.Sites.FirstOrDefault()?.Buildings.FirstOrDefault() as Xbim.Ifc4.ProductExtension.IfcBuilding;
                var MaxStdFlrNo = 0;
                //处理95%
                List<Tuple<int, double, double>> StoreyDic = new List<Tuple<int, double, double>>();
                foreach (Xbim.Ifc4.ProductExtension.IfcBuildingStorey BuildingStorey in bigBuildings.BuildingStoreys)
                {
                    double Storey_Elevation = BuildingStorey.Elevation.Value;
                    double Storey_Height = double.Parse(((BuildingStorey.PropertySets.FirstOrDefault().PropertySetDefinitions.FirstOrDefault() as Xbim.Ifc4.Kernel.IfcPropertySet).HasProperties.FirstOrDefault(o => o.Name == "Height") as Xbim.Ifc4.PropertyResource.IfcPropertySingleValue).NominalValue.Value.ToString());
                    StoreyDic.Add((int.Parse(BuildingStorey.Name.ToString()), Storey_Elevation, Storey_Height).ToTuple());
                    //获取标准层信息，因为不是所有的IFC都会符合我们约定俗成的"标准"，所以这里加上try...catch
                    //即符合我们标准的我们获取，不符合的"跳过"
                    try
                    {
                        var property = BuildingStorey.Model.Instances.OfType<Xbim.Ifc4.Kernel.IfcRelDefinesByProperties>().FirstOrDefault(o => o.RelatedObjects.Contains(BuildingStorey));
                        if (property != null)
                        {
                            var stdFlrNoPrp = (property.RelatingPropertyDefinition as Xbim.Ifc4.Kernel.IfcPropertySet).HasProperties
                                .Where(o => o.Name.Equals("StdFlrNo")).FirstOrDefault()
                                as Xbim.Ifc4.PropertyResource.IfcPropertySingleValue;
                            var stdFlrNo = int.Parse(stdFlrNoPrp.NominalValue.ToString());
                            MaxStdFlrNo = Math.Max(stdFlrNo, MaxStdFlrNo);
                        }
                    }
                    catch
                    {
                        // do not
                    }
                }
                StoreyDic = StoreyDic.OrderBy(x => x.Item1).ToList();

                //处理5%
                var definitions = suProject.Definitions;
                var storeys = new List<Xbim.Ifc4.ProductExtension.IfcBuildingStorey>();
                foreach (var BuildingStorey in suProject.Building.Storeys)
                {
                    var bigStorey = StoreyDic.FirstOrDefault(o => o.Item1 == BuildingStorey.Number);
                    double Storey_z = 0;
                    if (bigStorey == null)
                    {
                        Storey_z = BuildingStorey.Elevation;
                        bigStorey = StoreyDic.FirstOrDefault(o => Math.Abs(o.Item2 - Storey_z) <= 200);
                        if (bigStorey == null)
                        {
                            if (Math.Abs(Storey_z - (StoreyDic.Last().Item2 + StoreyDic.Last().Item3)) <= 200)
                            {
                                //楼层高度 = 最顶层的标高 + 最顶层的层高，说明这个是新的一层
                                var storeyNo = StoreyDic.Last().Item1 + 1;
                                StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                                bigStorey = StoreyDic.Last();
                            }
                            else if (Storey_z < StoreyDic.First().Item2)
                            {
                                var storeyNo = StoreyDic.First().Item1 - 1;
                                if (storeyNo == 0)
                                {
                                    storeyNo--;
                                }
                                StoreyDic.Insert(0, (storeyNo, Storey_z, StoreyDic.First().Item2 - Storey_z).ToTuple());
                                bigStorey = StoreyDic.First();
                            }
                            else if (Storey_z > (StoreyDic.Last().Item2 + StoreyDic.Last().Item3))
                            {
                                var storeyNo = StoreyDic.Last().Item1 + 1;
                                StoreyDic.Add((storeyNo, Storey_z, 0.0).ToTuple());
                                bigStorey = StoreyDic.Last();
                            }
                            else
                            {
                                bigStorey = StoreyDic.FirstOrDefault(o => Storey_z - o.Item2 > -200);
                            }
                        }
                    }
                    var storeyName = bigStorey.Item1.ToString().Replace('-', 'B');
                    var storey = bigBuildings.BuildingStoreys.FirstOrDefault(o => o.Name==storeyName) as Xbim.Ifc4.ProductExtension.IfcBuildingStorey;
                    if (storey == null)
                    {
                        storey = ThProtoBuf2IFC4Factory.CreateStorey(bigModel, bigBuildings, BuildingStorey);
                        storeys.Add(storey);
                    }
                    var suElements = new List<Xbim.Ifc4.ProductExtension.IfcBuildingElement>();
                    foreach (var element in BuildingStorey.Buildings)
                    {
                        var def = definitions[element.Component.DefinitionIndex];
                        Xbim.Ifc4.ProductExtension.IfcBuildingElement ifcBuildingElement;
                        ifcBuildingElement = ThProtoBuf2IFC4Factory.CreatedSUElement(bigModel, def, element.Component);
                        suElements.Add(ifcBuildingElement);
                    }
                    using (var txn = bigModel.BeginTransaction("relContainEntitys2Storey"))
                    {
                        //for ifc2x3
                        var relContainedIn = bigModel.Instances.New<Xbim.Ifc4.ProductExtension.IfcRelContainedInSpatialStructure>();
                        storey.ContainsElements.Append<Xbim.Ifc4.Interfaces.IIfcRelContainedInSpatialStructure>(relContainedIn);

                        relContainedIn.RelatingStructure = storey;
                        relContainedIn.RelatedElements.AddRange(suElements);
                        txn.Commit();
                    }
                }
                // IfcRelAggregates 关系
                ThProtoBuf2IFC4RelAggregatesFactory.Create(bigModel, bigBuildings, storeys);
                //返回
                return bigModel;
            }
            else
            {
                throw new NotSupportedException("Not Support this IFC Version");
            }
        }

        /// <summary>
        /// 结构和建筑合模(合模IFC内部的多个Site)
        /// </summary>
        public IfcStore ModelMerge(THDocument currentDocument, IfcStore model)
        {
            var modelProject = model.Instances.FirstOrDefault<Xbim.Ifc2x3.Kernel.IfcProject>();
            if(modelProject == null)
            {
                return null;
            }
            var NewModel = ThProtoBuf2IFC2x3Factory.CreateAndInitModel(modelProject.Name);
            var site = ThProtoBuf2IFC2x3Factory.CreateSite(NewModel);
            IfcBuilding building = ThProtoBuf2IFC2x3Factory.CreateBuilding(NewModel, site, "Default Building");
            var allFloors = ProjectExtension.AllProjectStoreys(currentDocument.AllBimProjects);
            var storeys = new List<IfcBuildingStorey>();
            //foreach (var floor in allFloors)
            //{
            //    var storey = ThProtoBuf2IFC2x3Factory.CreateStorey(NewModel, building, floor);
            //    storeys.Add(storey);
            //}
            foreach (IfcBuildingStorey floor in modelProject.Sites.First().Buildings.First().BuildingStoreys)
            {
                var storey = ThProtoBuf2IFC2x3Factory.CreateStorey(NewModel, building, floor);
                storeys.Add(storey);
            }
            // IfcRelAggregates 关系
            ThProtoBuf2IFC2x3RelAggregatesFactory.Create(NewModel, building, storeys);
            #region Insert Copy Function
            PropertyTranformDelegate semanticFilter = (property, parentObject) =>
            {
                return property.PropertyInfo.GetValue(parentObject, null);
            };
            //single map should be used for all insertions between two models
            var map = new XbimInstanceHandleMap(model, NewModel);
            #endregion

            
            var modelSites = modelProject.Sites;
            var relVoidsElements = model.Instances.OfType<IfcRelVoidsElement>();//洞口
            var relFillsElements = model.Instances.OfType<IfcRelFillsElement>();//洞口内包含的构建
            foreach (var modelSite in modelSites)
            {
                var modelBuilding = modelSite.Buildings.FirstOrDefault() as IfcBuilding;
                foreach (IfcBuildingStorey BuildingStorey in modelBuilding.BuildingStoreys)
                {
                    IfcBuildingStorey storey = (IfcBuildingStorey)building.BuildingStoreys.FirstOrDefault(o => Math.Abs(o.Elevation.Value - BuildingStorey.Elevation.Value) < 200);
                    if (storey != null)
                    {
                        var CreatElements = new List<Xbim.Ifc2x3.Kernel.IfcProduct>();
                        foreach (var spatialStructure in BuildingStorey.ContainsElements)
                        {
                            var elements = spatialStructure.RelatedElements;
                            using (var txn = NewModel.BeginTransaction("Insert copy"))
                            {
                                foreach(var e in elements)
                                {
                                    if (e is Xbim.Ifc2x3.SharedBldgElements.IfcWall wall)
                                    {
                                        var newWall = NewModel.InsertCopy(wall, map, semanticFilter, false, false);
                                        var wall_relVoidsElements = relVoidsElements.Where(o => o.RelatingBuildingElement == wall);
                                        foreach (var voildElementHole in wall_relVoidsElements)
                                        {
                                            var hole = voildElementHole.RelatedOpeningElement as IfcOpeningElement;
                                            var newHole = NewModel.InsertCopy(hole, map, semanticFilter, false, false);
                                            //ThProtoBuf2IFC2x3Builder.SetupHole(NewModel, storey, newWall, newHole);
                                            var relVoidsElement = NewModel.Instances.New<IfcRelVoidsElement>();
                                            relVoidsElement.RelatedOpeningElement = newHole;
                                            relVoidsElement.RelatingBuildingElement = newWall;

                                            var relFillsElement = relFillsElements.FirstOrDefault(o => o.RelatingOpeningElement == hole);
                                            if(relFillsElement != null)
                                            {
                                                var ifcBuildingElement = relFillsElement.RelatedBuildingElement;
                                                var newBuildingElement = NewModel.InsertCopy(ifcBuildingElement, map, semanticFilter, false, false);
                                                var FillsElement = NewModel.Instances.New<IfcRelFillsElement>();
                                                FillsElement.RelatingOpeningElement = newHole;
                                                FillsElement.RelatedBuildingElement = newBuildingElement;
                                                CreatElements.Add(newBuildingElement);
                                            }
                                        }
                                        CreatElements.Add(newWall);
                                    }
                                    else if (e is Xbim.Ifc2x3.SharedBldgElements.IfcDoor || e is Xbim.Ifc2x3.SharedBldgElements.IfcWindow)
                                    {
                                        //do not
                                    }
                                    else
                                    {
                                        var newElement = NewModel.InsertCopy(e, map, semanticFilter, false, false);
                                        CreatElements.Add(newElement);
                                    }
                                }
                                txn.Commit();
                            }
                        }
                        using (var txn = NewModel.BeginTransaction("relContainEntitys2Storey"))
                        {
                            //for ifc2x3
                            var relContainedIn = NewModel.Instances.New<IfcRelContainedInSpatialStructure>();
                            storey.ContainsElements.Append<Xbim.Ifc2x3.Interfaces.IIfcRelContainedInSpatialStructure>(relContainedIn);

                            relContainedIn.RelatingStructure = storey;
                            relContainedIn.RelatedElements.AddRange(CreatElements);
                            txn.Commit();
                        }
                    }
                }
            }


            return NewModel;
        }
    }
}
