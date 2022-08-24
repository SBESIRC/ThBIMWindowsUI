using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;

namespace XbimXplorer.ThBIMEngine
{
    class ThBimFilterController
    {
		public Dictionary<string, List<FilterBase>> PrjAllFilters { get; }
		public ThBimFilterController() 
		{
			PrjAllFilters = new Dictionary<string, List<FilterBase>>();
		}
		public void UpdataProjectFilter()
		{
			PrjAllFilters.Clear();
			var storeyFilters = ProjectExtension.GetProjectStoreyFilters(THBimScene.Instance.AllBimProjects);
			var typeFilters = ProjectExtension.GetProjectTypeFilters(THBimScene.Instance.AllBimProjects);
			foreach (var project in THBimScene.Instance.AllBimProjects)
			{
				var filter = new ProjectFilter(new List<string> { project.ProjectIdentity });
				var listFilters = new List<FilterBase>();
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
				ProjectExtension.PorjectFilterEntitys(project, listFilters);
				PrjAllFilters.Add(project.ProjectIdentity, listFilters);
			}
		}

		public List<int> GetGlobalIndexByFilter(Dictionary<string, HashSet<string>> prjFilterIds)
		{
			var resIds = new HashSet<int>();
			Parallel.ForEach(THBimScene.Instance.MeshEntiyRelationIndexs.Values, new ParallelOptions() { }, item =>
			   {
				   var prjEntityId = item.ProjectEntityId;
				   if (!prjFilterIds.ContainsKey(item.ProjectEntityId))
					   return;
				   var filterIds = prjFilterIds[prjEntityId];
				   if (filterIds.Contains(prjEntityId))
					   resIds.Add(item.GlobalMeshIndex);
			   });
			return resIds.ToList();
		}

		public void ShowEntityByFilter() 
		{
		
		}
	}
}
