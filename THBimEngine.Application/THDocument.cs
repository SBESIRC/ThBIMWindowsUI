using System;
using System.Collections.Generic;
using System.ComponentModel;
using THBimEngine.Domain;
using Xbim.Common.Geometry;
using Xbim.Ifc;

namespace THBimEngine.Application
{
    public class THDocument
	{
		/// <summary>
		/// DocumentId对应项目子项的Id
		/// </summary>
		public string DocumentId { get; }
		/// <summary>
		/// 
		/// </summary>
		public string DocumentName { get; set; }
		#region
		private DocumentProjectEngine projectEngine;
		#endregion
		public THDocument(string id, string name,ProgressChangedEventHandler progress)
		{
			DocumentId = id;
			DocumentName = name;
			AllBimProjects = new List<THBimProject>();
			AllStoreys = new Dictionary<string, THBimStorey>();
			AllGeoModels = new List<GeometryMeshModel>();
			AllGeoPointNormals = new List<PointNormal>();
			MeshEntiyRelationIndexs = new Dictionary<int, MeshEntityIdentifier>();
			UnShowEntityTypes = new List<string>();
			projectEngine = new DocumentProjectEngine(progress);
            this.DocumentChanged += THDocument_DocumentChanged;
		}
        public List<string> UnShowEntityTypes { get; }
		/// <summary>
		/// 所有项目
		/// </summary>
		public List<THBimProject> AllBimProjects { get; }
		/// <summary>
		/// 所有物体的三角面片信息集合
		/// </summary>
		public List<GeometryMeshModel> AllGeoModels { get; }
		/// <summary>
		/// 所有物体的顶点集合
		/// </summary>
		public List<PointNormal> AllGeoPointNormals { get; }
		/// <summary>
		/// 所有项目中的楼层集合
		/// </summary>
		public Dictionary<string, THBimStorey> AllStoreys { get; }
		/// <summary>
		/// 项目中的所有Project中Entity和的编号和Entity对应的关系
		/// Index是渲染引擎使用
		/// </summary>
		public Dictionary<int, MeshEntityIdentifier> MeshEntiyRelationIndexs { get; }//relationId

		#region 对外方法
		public bool IsAddProject(string prjIdentity)
		{
			bool isAdd = true;
			foreach (var item in AllBimProjects)
			{
				if (item.ProjectIdentity == prjIdentity)
				{
					isAdd = false;
					break;
				}
			}
			return isAdd;
		}
		/// <summary>
		/// 加入项目
		/// </summary>
		/// <param name="project"></param>
		/// <param name="matrix3d"></param>
		public void AddProject(object project, XbimMatrix3D? matrix3d)
		{
			if (project == null)
				return;
			XbimMatrix3D projectMatrix3D = matrix3d.HasValue? matrix3d.Value: XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
			if (project is IfcStore ifcStore)
			{
				projectEngine.AddProject(this, ifcStore, projectMatrix3D);
			}
			else if (project is ThTCHProjectData thTCHProject)
			{
				projectEngine.AddProject(this, thTCHProject, projectMatrix3D);
			}
			else if (project is ThSUProjectData thSUProject)
			{
				projectEngine.AddProject(this, thSUProject, projectMatrix3D);
			}
			else 
			{
				throw new NotSupportedException();
			}
			if (projectEngine.HaveChange)
			{
				projectEngine.HaveChange = false;
				DocumentChanged.Invoke(this, null); 
			}
		}
		/// <summary>
		/// 清除项目的所有数据
		/// </summary>
		public void ClearAllData()
		{
			AllBimProjects.Clear();
			AllStoreys.Clear();
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
			DocumentChanged.Invoke(this, null);
		}
		/// <summary>
		/// 删除项目
		/// </summary>
		/// <param name="prjIdentity"></param>
		public void DeleteProject(string prjIdentity)
		{
			THBimProject delPrj = null;
			foreach (var project in AllBimProjects)
			{
				if (project.ProjectIdentity != prjIdentity)
					continue;
				delPrj = project;
				//删除楼层记录，要删除的实体
				if (project.ProjectSite == null || project.ProjectSite.SiteBuildings.Count < 1)
					continue;
				foreach (var build in project.ProjectSite.SiteBuildings)
				{
					foreach (var storey in build.Value.BuildingStoreys)
					{
						AllStoreys.Remove(storey.Key);
					}
				}
			}
			if (null != delPrj)
			{
				AllBimProjects.Remove(delPrj);
				delPrj = null;
				DocumentChanged.Invoke(this, null);
			}
		}
		/// <summary>
		/// 获取实体的Model
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public object GetEntityByIndex(int index) 
		{
			if (!MeshEntiyRelationIndexs.ContainsKey(index))
				return null;
			var meshRelation = MeshEntiyRelationIndexs[index];
			foreach (var project in AllBimProjects)
			{
				if (project.ProjectIdentity != meshRelation.ProjectId)
					continue;
				if (project.SourceProject != null && project.SourceProject is IfcStore ifcStore)
				{
					var ifcLable = Convert.ToInt32(meshRelation.ProjectEntityId);
					var ifcEntity = ifcStore.Instances[ifcLable];
					return ifcEntity;
				}
				else
				{
					var relaton = project.PrjAllRelations[meshRelation.ProjectEntityId];
					var entity = project.PrjAllEntitys[relaton.RelationElementUid];
					return entity;
				}
			}
			return null;
		}
		/// <summary>
		/// 触发文档修改事件
		/// </summary>
		public void NotifyDocumentChanged() 
		{
			DocumentChanged.Invoke(this, null);
		}
		#endregion

        #region 对外事件
        /// <summary>
        /// Document修改事件
        /// </summary>
        public event EventHandler DocumentChanged;
		#endregion
		
		private void THDocument_DocumentChanged(object sender, EventArgs e)
		{
			UpdateCatchStoreyRelation();
            ReadGeometryMesh();
			ReadProjectGrid();
		}
		private void UpdateCatchStoreyRelation()
		{
			AllStoreys.Clear();
			foreach (var project in AllBimProjects)
			{
				foreach (var item in project.PrjAllStoreys)
				{
					if (AllStoreys.ContainsKey(item.Key))
						continue;
					AllStoreys.Add(item.Key, item.Value);
				}
			}
		}
		private void ReadGeometryMesh()
		{
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
			var meshResult = new GeometryMeshResult();
			foreach (var project in AllBimProjects)
			{
				int ptOffSet = meshResult.AllGeoPointNormals.Count;
				int gOffSet = meshResult.AllGeoModels.Count;
				var models = project.AllGeoModels().Values;
				var points = project.AllGeoPointNormals();
				foreach (var item in points)
				{
					item.PointIndex += ptOffSet;
				}
				foreach (var item in models)
				{
					item.CIndex = gOffSet;
					foreach (var tr in item.FaceTriangles)
					{
						for (int i = 0; i < tr.ptIndex.Count; i++)
							tr.ptIndex[i] += ptOffSet;
					}
					MeshEntiyRelationIndexs.Add(item.CIndex, new MeshEntityIdentifier(item.CIndex, project.ProjectIdentity, item.EntityLable));
					gOffSet += 1;
				}
				meshResult.AllGeoPointNormals.AddRange(points);
				meshResult.AllGeoModels.AddRange(models);
			}
			AllGeoPointNormals.AddRange(meshResult.AllGeoPointNormals);
			AllGeoModels.AddRange(meshResult.AllGeoModels);
		}
		private void ReadProjectGrid() 
		{
			var startGIndex = AllGeoModels.Count + 1;
			foreach (var prj in AllBimProjects)
			{
				foreach (var item in prj.PrjAllEntitys.Values)
				{
					if (item.GetType().Name == "GridLine")
					{
						MeshEntiyRelationIndexs.Add(startGIndex, new MeshEntityIdentifier(startGIndex, prj.ProjectIdentity, item.Uid));
						startGIndex += 1;
					}
					if (item.GetType().Name == "GridCircle")
					{
						MeshEntiyRelationIndexs.Add(startGIndex, new MeshEntityIdentifier(startGIndex, prj.ProjectIdentity, item.Uid));
						startGIndex += 1;
					}
					if (item.GetType().Name == "GridText")
					{
						MeshEntiyRelationIndexs.Add(startGIndex, new MeshEntityIdentifier(startGIndex, prj.ProjectIdentity, item.Uid));
						startGIndex += 1;
					}
				}
			}
		}
	}
}
