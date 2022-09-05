using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;
using THBimEngine.Presention;

namespace XbimXplorer.ThBIMEngine
{
    class ThBimFilterController
    {
		public Dictionary<string, List<FilterBase>> PrjAllFilters { get; }
		public HashSet<int> ShowEntityGIndex { get; protected set; }
		private int AllEntityCount = 0;
		public ThBimFilterController() 
		{
			PrjAllFilters = new Dictionary<string, List<FilterBase>>();
			ShowEntityGIndex = new HashSet<int>();
		}
		public void UpdataProjectFilter() // 预缓存
		{
			PrjAllFilters.Clear();
			ShowEntityGIndex.Clear();
			ShowEntityGIndex = THBimScene.Instance.MeshEntiyRelationIndexs.Keys.ToHashSet();
			AllEntityCount = ShowEntityGIndex.Count;
			var storeyFilters = ProjectExtension.GetProjectStoreyFilters(THBimScene.Instance.AllBimProjects); // 获取所有的 storey filter
			var typeFilters = ProjectExtension.GetProjectTypeFilters(THBimScene.Instance.AllBimProjects); // 获取所有的 type filter
			foreach (var project in THBimScene.Instance.AllBimProjects)
			{
				var filter = new ProjectFilter(new List<string> { project.ProjectIdentity });
				filter.Describe = project.Name;
				var listFilters = new List<FilterBase>();
				listFilters.Add(filter);
				foreach (var storeyFilter in storeyFilters)
				{
					var copyItem = storeyFilter.Clone() as StoreyFilter;
					listFilters.Add(copyItem);
				}
				foreach (var typeFilter in typeFilters)
				{
					var copyItem = typeFilter.Clone() as TypeFilter;
					listFilters.Add(copyItem);
				}
				ProjectExtension.PorjectFilterEntitys(project, listFilters); // 获取所有的数据
				PrjAllFilters.Add(project.ProjectIdentity, listFilters);
			}

			UnFilter();
		}

		public HashSet<int> GetGlobalIndexByFilterIds(Dictionary<string, HashSet<string>> prjFilterIds)
		{
			var resIds = new HashSet<int>();
			Parallel.ForEach(THBimScene.Instance.MeshEntiyRelationIndexs.Values, new ParallelOptions() { }, item =>
			   {
				   var prjId = item.ProjectId;
				   if (!prjFilterIds.ContainsKey(prjId))
					   return;
				   var prjEntityId = item.ProjectEntityId;
				   var filterIds = prjFilterIds[prjId];
				   if (filterIds.Contains(prjEntityId))
				   {
					   lock (resIds) 
					   {
						   if(!resIds.Contains(item.GlobalMeshIndex))
							   resIds.Add(item.GlobalMeshIndex);
					   }
				   }
			   });
			return resIds;
		}
		public Dictionary<string,HashSet<string>> GetProjectFilterStrIds(Dictionary<string, List<FilterBase>> prjFilters)
		{
			var res = new Dictionary<string, HashSet<string>>();
			foreach (var item in prjFilters) 
			{
				var filters = item.Value;
				var filterIds = new HashSet<string>();
				foreach (var filter in filters)
				{
					foreach (var id in filter.ResultElementUids)
					{
						if (filterIds.Contains(id))
							continue;
						filterIds.Add(id);
					}
				}
				res.Add(item.Key, filterIds);
			}
			return res;
		}
		public Dictionary<string, HashSet<string>> GetProjectFilterStrIds(Dictionary<string, FilterBase> prjFilters)
		{
			var res = new Dictionary<string, HashSet<string>>();
			foreach (var item in prjFilters)
			{
				var filter = item.Value;
				var filterIds = new HashSet<string>();
				foreach (var id in filter.ResultElementUids)
				{
					if (filterIds.Contains(id))
						continue;
					filterIds.Add(id);
				}
				res.Add(item.Key, filterIds);
			}
			return res;
		}
		public HashSet<int> GetGlobalIndexByFilter(Dictionary<string, List<FilterBase>> prjFilters)
		{
			var filterIds = GetProjectFilterStrIds(prjFilters);
			return GetGlobalIndexByFilterIds(filterIds);
		}
		public HashSet<int> GetGlobalIndexByFilter(Dictionary<string, FilterBase> prjFilters)
		{
			var filterIds = GetProjectFilterStrIds(prjFilters);
			return GetGlobalIndexByFilterIds(filterIds);
		}
		public void ShowEntityByFilter(HashSet<int> delIds,HashSet<int> addIds) 
		{
			// 在此传输数据给viewer
			ShowEntityGIndex = ShowEntityGIndex.Except(delIds).ToHashSet();
			ShowEntityGIndex = ShowEntityGIndex.Union(addIds).ToHashSet();
			EntityShowByIds();
		}
		public void ShowEntity(HashSet<int> showIds)
		{
			// 在此传输数据给viewer
			ShowEntityGIndex = showIds;
			EntityShowByIds();
		}
		private void EntityShowByIds() 
		{
			ExampleScene.ifcre_set_config("to_show_states", "0");
			ExampleScene.ifcre_set_comp_ids(-1);
			//if (ShowEntityGIndex.Count() == AllEntityCount)
			//{
			//	UnFilter();
			//	return;
			//}
			
			ExampleScene.ifcre_set_sleep_time(100);
			foreach (var id in ShowEntityGIndex)
			{
				ExampleScene.ifcre_set_comp_ids(id);
			}
			ExampleScene.ifcre_set_sleep_time(10);
		}

		private void UnFilter() 
		{
			return;
			ExampleScene.ifcre_set_comp_ids(-2);
		}
	}
}
