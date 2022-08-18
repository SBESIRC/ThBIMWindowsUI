﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;
using Xbim.Ifc;

namespace THBimEngine.Domain
{
    public class THBimScene
    {
		THBimScene() 
		{
			AllBimProjects = new List<THBimProject>();
			AllStoreys = new Dictionary<string, THBimStorey>();
			AllEntitys = new Dictionary<string, THBimEntity>();
			AllRelations = new Dictionary<string, THBimElementRelation>();
			AllGeoModels = new List<GeometryMeshModel>();
			AllGeoPointNormals = new List<PointNormal>();
			MeshEntiyRelationIndexs = new Dictionary<int, string>();
			UnShowEntityTypes = new List<string>();
		}
		public static readonly THBimScene Instance = new THBimScene();
		public List<string> UnShowEntityTypes { get; }
		/// <summary>
		/// 所有项目
		/// </summary>
		public List<THBimProject> AllBimProjects { get; }
		/// <summary>
		/// 所有项目中的楼层集合
		/// </summary>
		public Dictionary<string, THBimStorey> AllStoreys { get; }
		/// <summary>
		/// 所有项目所有楼层中的的关联的实体信息
		/// （通过RelationElementUID）找Entity
		/// </summary>
		public Dictionary<string, THBimElementRelation> AllRelations { get; }
		/// <summary>
		/// 所有非重复的实体的信息（一个实体可以通过关联+转换到其它位置）
		/// </summary>
		public Dictionary<string, THBimEntity> AllEntitys { get; }
		/// <summary>
		/// 所有物体的三角面片信息集合
		/// </summary>
		public List<GeometryMeshModel> AllGeoModels { get; }
		/// <summary>
		/// 所有物体的顶点集合
		/// </summary>
		public List<PointNormal> AllGeoPointNormals { get; }
		Dictionary<int, string> MeshEntiyRelationIndexs { get; }//relationId
		public void ClearAllData() 
		{
			AllBimProjects.Clear();
			AllStoreys.Clear();
			AllEntitys.Clear();
			AllRelations.Clear();
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
		}
		public void DeleteProjectData(string prjIdentity)
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
						foreach (var item in storey.Value.FloorEntitys)
						{
							AllEntitys.Remove(item.Key);
						}
						AllStoreys.Remove(storey.Key);
					}
				}
			}
			if (null != delPrj)
				AllBimProjects.Remove(delPrj);
			delPrj = null;
		}
		public void UpdateCatchStoreyRelation() 
		{
			AllStoreys.Clear();
			AllRelations.Clear();
			AllEntitys.Clear();

			foreach (var project in AllBimProjects)
			{
				if (project.HaveChange)
				{
					project.UpdateCatchStoreyRelation();
					project.UnShowEntityTypes.Clear();
					foreach (var item in UnShowEntityTypes)
						project.UnShowEntityTypes.Add(item);
				}
				foreach (var item in project.PrjAllStoreys) 
				{
					AllStoreys.Add(item.Key,item.Value);
				}
				foreach (var item in project.PrjAllRelations)
				{
					AllRelations.Add(item.Key, item.Value);
				}
				foreach (var item in project.PrjAllEntitys) 
				{
					AllEntitys.Add(item.Key, item.Value);
				}
			}
		}
		public void ReadGeometryMesh() 
		{
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
			CheckUpdateCatchMesh();
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
					gOffSet += 1;
				}
				meshResult.AllGeoPointNormals.AddRange(points);
				meshResult.AllGeoModels.AddRange(models);
				project.HaveChange = false;
			}
			AllGeoPointNormals.AddRange(meshResult.AllGeoPointNormals);
			AllGeoModels.AddRange(meshResult.AllGeoModels);
		}
		private void CheckUpdateCatchMesh()
		{
			foreach (var project in AllBimProjects)
			{
				if (project.HaveChange)
				{
					project.UpdataGeometryMeshModel();
				}
			}
		}
	}
	public class GeometryMeshResult
	{
		public List<GeometryMeshModel> AllGeoModels { get; }
		public List<PointNormal> AllGeoPointNormals { get; }
		public GeometryMeshResult()
		{
			AllGeoModels = new List<GeometryMeshModel>();
			AllGeoPointNormals = new List<PointNormal>();
		}
	}
}
