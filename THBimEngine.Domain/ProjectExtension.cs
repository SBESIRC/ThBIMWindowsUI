using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace THBimEngine.Domain
{
    public static class ProjectExtension
    {
        public static List<Type> ProjrctEntityTypes(this THBimProject project) 
        {
            var types = new HashSet<Type>();
            if (null == project)
                return types.ToList();
            if (project.SourceProject != null && project.SourceProject is IfcStore ifcStore)
			{
                var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
                if (ifcProject.Sites == null || ifcProject.Sites.Count() < 1)
                    return types.ToList();
                var site = ifcProject.Sites.First();
                foreach (var building in site.Buildings)
                {
                    foreach (var ifcStorey in building.BuildingStoreys)
                    {
                        var storey = ifcStorey as Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey;
                        foreach (var spatialStructure in storey.ContainsElements)
                        {
                            var elements = spatialStructure.RelatedElements;
                            if (elements.Count == 0) 
                                continue;
                            foreach (var item in elements) 
                            {
                                var ifcType = item.GetType();
                                if (!types.Contains(ifcType))
                                    types.Add(ifcType);
                            }
                        }
                    }
                }
            }
			else
			{
				var allTypes = project.PrjAllEntitys.Values.Select(c => c.GetType()).ToList();
                foreach (var item in allTypes) 
                {
                    if (types.Contains(item))
                        continue;
                    types.Add(item);
                }
			}
			return types.ToList();
        }
        public static List<THBimStorey> ProjectAllStorey(this THBimProject project) 
        {
            var storeys = new List<THBimStorey>();
            if (project.SourceProject != null && project.SourceProject is IfcStore ifcStore)
            {
                var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
                if (null == project)
                    return storeys;
                if (ifcProject.Sites == null || ifcProject.Sites.Count() < 1)
                    return storeys;
                var site = ifcProject.Sites.First();
                foreach (var building in site.Buildings)
                {
                    foreach (var ifcStorey in building.BuildingStoreys)
                    {
                        if (ifcStorey is Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey storey23)
                        {
                            var bimStorey = IfcStoreyToBimStorey(storey23);
                            storeys.Add(bimStorey);
                        }
                        else if (ifcStorey is Xbim.Ifc4.ProductExtension.IfcBuildingStorey storey4) 
                        {
                            var bimStorey = IfcStoreyToBimStorey(storey4);
                            storeys.Add(bimStorey);
                        }
                    }
                }
            }
            else
            {
                foreach (var item in project.ProjectSite.SiteBuildings) 
                {
                    storeys.AddRange(item.Value.BuildingStoreys.Values);
                }
            }
            return storeys;
        }
        public static Dictionary<string, List<Type>> AllProjectTypes(List<THBimProject> allProjects) 
        {
            var resTypes = new Dictionary<string, List<Type>>();
            foreach (var project in allProjects) 
            {
                var prjTypes = project.ProjrctEntityTypes(); 
                foreach (var item in prjTypes)
                {
                    var realName = "未知";
                    var name = item.Name.ToLower();
                    if (name.Contains("wall"))
                    {
                        realName = "墙";
                    }
                    else if (name.Contains("beam"))
                    {
                        realName = "梁";
                    }
                    else if (name.Contains("slab"))
                    {
                        realName = "板";
                    }
                    else if (name.Contains("column"))
                    {
                        realName = "柱";
                    }
                    else if (name.Contains("door"))
                    {
                        realName = "门";
                    }
                    else if (name.Contains("window"))
                    {
                        realName = "窗";
                    }
                    else if (name.Contains("openging"))
                    {
                        realName = "洞口";
                    }
                    if (resTypes.ContainsKey(realName))
                    {
                        resTypes[realName].Add(item);
                    }
                    else
                    {
                        resTypes.Add(realName, new List<Type> { item });
                    }
                }
            }
            return resTypes;
        }
        public static Dictionary<string, List<THBimStorey>> AllProjectStoreys(List<THBimProject> allProjects) 
        {
            var resStoreys = new Dictionary<string, List<THBimStorey>>();
            foreach (var project in allProjects) 
            {
                var storeys = project.ProjectAllStorey();
                foreach (var storey in storeys) 
                {
                    if (resStoreys.ContainsKey(storey.Name)) 
                    {
                        resStoreys[storey.Name].Add(storey);
                    }
                    else 
                    {
                        resStoreys.Add(storey.Name, new List<THBimStorey> { storey });
                    }
                }
            }
            return resStoreys;
        }

        public static List<TypeFilter> GetProjectTypeFilters(List<THBimProject> allProjects) 
        {
            var filters = new List<TypeFilter>();
            var allTypes = AllProjectTypes(allProjects);
            foreach (var item in allTypes) 
            {
                var newFilter = new TypeFilter(item.Value);
                newFilter.Describe = item.Key;
                filters.Add(newFilter);
            }
            return filters;
        }
        public static List<StoreyFilter> GetProjectStoreyFilters(List<THBimProject> allProjects)
        {
            var filters = new List<StoreyFilter>();
            var allStoreys = AllProjectStoreys(allProjects); // storeyId--eachProjectStorey
            foreach (var item in allStoreys)
            {
                var newFilter = new StoreyFilter(item.Value.Select(c=>c.Uid).ToList());
                newFilter.Describe = item.Key;
                filters.Add(newFilter);
            }
            return filters;
        }

        public static List<ProjectFilter> GetProjectFilters(List<THBimProject> allProjects) 
        {
            var filters = new List<ProjectFilter>();
            foreach (var item in allProjects) 
            {
                var filter = new ProjectFilter(new List<string> { item.ProjectIdentity });
                filters.Add(filter);
            }
            return filters;
        }
        public static THBimStorey IfcStoreyToBimStorey(Xbim.Ifc2x3.ProductExtension.IfcBuildingStorey storey)
        {
            double storey_Elevation = 0;
            double storey_Height = 0;
            if (!(storey.Elevation is null))
            {
                storey_Elevation = storey.Elevation.Value;
            }
            var bimStorey = new THBimStorey(storey.EntityLabel, storey.Name, storey_Elevation, storey_Height, "", storey.GlobalId);
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
        public static THBimStorey IfcStoreyToBimStorey(Xbim.Ifc4.ProductExtension.IfcBuildingStorey storey)
        {
            double storey_Elevation = 0;
            double storey_Height = 0;
            if (!(storey.Elevation is null))
            {
                storey_Elevation = storey.Elevation.Value;
            }
            var bimStorey = new THBimStorey(storey.EntityLabel, storey.Name, storey_Elevation, storey_Height, "", storey.GlobalId);
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

        public static void PorjectFilterEntitys(this THBimProject project,List<FilterBase> allFilters) 
        {
            bool validFilter = false;
            foreach (var filter in allFilters)
            {
                if (filter.CheckProject(project))
                {
                    validFilter = true;
                }
            }
            if (!validFilter) { return; }
            if (project.SourceProject != null && project.SourceProject is IfcStore ifcStore)
            {
                var ifcProject = ifcStore.Instances.FirstOrDefault<IIfcProject>();
                if (ifcProject.Sites == null || ifcProject.Sites.Count() < 1) { return; }
                var site = ifcProject.Sites.First(); // 对项目的每一个site遍历
                foreach (var building in site.Buildings) // 遍历site中的每一个building
                {
                    foreach (var ifcStorey in building.BuildingStoreys) // 遍历building中的每一楼层
                    {
                        //check valid
                        var storeyId = ifcStorey.GlobalId; // 楼层id
                        var storey = project.PrjAllStoreys[storeyId]; // 通过PrjAllStoreys找到这个楼层
                        //validFilter = false;
                        foreach (var filter in allFilters)
                        {
                            if (!filter.ProjectValid) { continue; }
                            filter.CheckStory(storey);
                        }
                        foreach (var spatialStructure in ifcStorey.ContainsElements) // 遍历楼层中每一个构件
                        {
                            var elements = spatialStructure.RelatedElements;
                            if (elements.Count == 0) { continue; }
                            foreach (var item in elements)
                            {
                                var ifcType = item.GetType();
                                foreach (var filter in allFilters)
                                {
                                    if (!filter.ProjectValid || !filter.StoreyValid) { continue; }
                                    if (filter.CheckType(ifcType))
                                        filter.ResultElementUids.Add(item.EntityLabel.ToString());
                                }
                            }
                        }
                    }
                }
            }
            else 
            {
                if (project.ProjectSite == null || project.ProjectSite.SiteBuildings.Count < 1) { return; }
                validFilter = false;
                foreach (var building in project.ProjectSite.SiteBuildings)  // 遍历site中的每一个building
                {
                    //check valid
                    foreach (var filter in allFilters)
                    {
                        if (!filter.ProjectValid || !filter.SiteValid) { continue; }
                        validFilter = true;
                        filter.CheckBuilding(building.Value);
                    }
                    if (!validFilter) { break; }
                    validFilter = false;

                    foreach (var story in building.Value.BuildingStoreys) // 遍历building中的每一楼层
                    {
                        //check valid
                        foreach (var filter in allFilters)
                        {
                            if (!filter.ProjectValid || !filter.SiteValid || !filter.BuildingValid) { continue; }
                            validFilter = true;
                            filter.CheckStory(story.Value);
                        }
                        if (!validFilter) { break; }

                        foreach (var relation in story.Value.FloorEntityRelations) // 遍历楼层中每一个构件
                        {
                            var entity = project.PrjAllEntitys[relation.Value.RelationElementUid];
                            foreach (var filter in allFilters)
                            {
                                if (!filter.ProjectValid || !filter.BuildingValid || !filter.StoreyValid) { continue; }
                                if (filter.CheckType(entity))
                                {
                                    filter.ResultElementUids.Add(relation.Key);
                                }
                            }
                        }
                    }
                }
            }
            
        }
    }
}
