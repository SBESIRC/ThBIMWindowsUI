using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;

namespace THBimEngine.Domain
{
    /// <summary>
    /// 项目
    /// 如果是IFC文件这里有用的数据是 ProjectIdentity Catagory SourceProject SourceName _allGeoMeshModels _allGeoPointNormals PrjAllStoreys
    /// IFC过来后不再处理其它的结构数据，只是缓存了实体的三角面片信息，项目的基本信息，SourceProject为IfcStore
    /// </summary>
    public class THBimProject : THBimElement,IEquatable<THBimProject>
    {
		/// <summary>
		/// 项目Id，唯一Id
		/// </summary>
        public string ProjectIdentity { get; set; }
		/// <summary>
		/// 项目分类
		/// </summary>
        public BuildingCatagory Catagory { get; set; }
        public THBimSite ProjectSite { get; set; }
		/// <summary>
		/// 原项目
		/// </summary>
        public object SourceProject { get; set; }
		/// <summary>
		/// 项目名称
		/// </summary>
		public string SourceName { get; set; }
		/// <summary>
		/// 是否需要根据创建的实体读取Mesh
		/// </summary>
		public bool NeedReadEntityMesh { get; set; }
		/// <summary>
		/// 缓存 - 本项目中的实体三角面片索引信息
		/// </summary>
		private Dictionary<string,GeometryMeshModel> _allGeoMeshModels { get; }
		/// <summary>
		/// 缓存 - 本项目中的实体三角面片点向量的信息
		/// </summary>
		private List<PointNormal> _allGeoPointNormals { get; }
		/// <summary>
		/// 项目中的实体
		/// </summary>
		public Dictionary<string,THBimEntity> PrjAllEntitys { get; }
		/// <summary>
		/// 项目中的显示实体和具体实体的关联关系（多个显示实体可以指向同一个实体，关系中有Transform进行转换）
		/// </summary>
		public Dictionary<string,THBimElementRelation> PrjAllRelations { get; }
		/// <summary>
		/// 项目中所有的楼层缓存（如果Id有重复的也只显示一个）
		/// </summary>
        public Dictionary<string, THBimStorey> PrjAllStoreys { get; }
		/// <summary>
		/// 不显示实体类别名称
		/// </summary>
		public List<string> UnShowEntityTypes { get; }
        public THBimProject(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
			NeedReadEntityMesh = true;
			_allGeoMeshModels = new Dictionary<string, GeometryMeshModel>();
			_allGeoPointNormals = new List<PointNormal>();
			PrjAllEntitys = new Dictionary<string, THBimEntity>();
			PrjAllRelations = new Dictionary<string, THBimElementRelation>();
			PrjAllStoreys = new Dictionary<string, THBimStorey>();
			UnShowEntityTypes = new List<string>();
            this.PorjectChanged += THBimProject_PorjectChanged;
		}
		public void ProjectChanged()
		{
			PorjectChanged.Invoke(this, null);
		}
		private void THBimProject_PorjectChanged(object sender, EventArgs e)
        {
			UpdateCatchStoreyRelation();
			if(NeedReadEntityMesh)
				UpdataGeometryMeshModel();
		}

        public override object Clone()
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(THBimProject other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
        public Dictionary<string,GeometryMeshModel> AllGeoModels()
        {
			var resList = new Dictionary<string,GeometryMeshModel>();
			foreach (var item in _allGeoMeshModels) 
			{
				resList.Add(item.Key,item.Value.Clone() as GeometryMeshModel);
			}
			return resList;
		}
        public List<PointNormal> AllGeoPointNormals()
        {
			var resList = new List<PointNormal>();
			foreach (var item in _allGeoPointNormals) 
			{
				var clonedPt = item.Clone();
				resList.Add(clonedPt);
			}
			return resList;
        }
		public void AddGeoMeshModels(List<GeometryMeshModel> meshModels, List<PointNormal> pointNormals) 
		{
			_allGeoMeshModels.Clear();
			_allGeoPointNormals.Clear();
			foreach (var item in meshModels)
			{
				_allGeoMeshModels.Add(item.EntityLable,item);
			}
			_allGeoPointNormals.AddRange(pointNormals);
			PorjectChanged.Invoke(this, null);
		}
		public void RemoveEntitys(THBimBuilding bimBuilding, List<string> rmEntityIds) 
		{
			var thisStoreys = bimBuilding.BuildingStoreys;
			if (null == rmEntityIds || rmEntityIds.Count < 1)
				return;
			var rmIds = new List<string>();
			foreach (var entityId in rmEntityIds)
			{
				if (!PrjAllEntitys.ContainsKey(entityId))
					continue;
				var entity = PrjAllEntitys[entityId];
				var pid = entity.ParentUid;
				while (!string.IsNullOrEmpty(pid) && !thisStoreys.ContainsKey(pid))
				{
					var pEntity = PrjAllEntitys[pid];
					pid = pEntity.ParentUid;
				}
				rmIds.Add(entityId);
				if (string.IsNullOrEmpty(pid))
					continue;
				foreach (var storeyKeyValue in thisStoreys)
				{
					var storey = storeyKeyValue.Value;
					if (storey.FloorEntitys.ContainsKey(entityId))
						storey.FloorEntitys.Remove(entityId);
					if (storey.Uid != pid && storey.MemoryStoreyId != pid)
						continue;
					var rmRealtion = storey.FloorEntityRelations.Where(c => c.Value.RelationElementUid == entityId).Select(c => c.Key).ToList();
					foreach (var rmId in rmRealtion)
					{
						storey.FloorEntityRelations.Remove(rmId);
					}
				}
			}
			foreach (var rmId in rmIds)
			{
				PrjAllEntitys.Remove(rmId);
			}
		}
		/// <summary>
		/// Document修改事件
		/// </summary>
		public event EventHandler PorjectChanged;
		public void ClearAllData()
		{
			PrjAllStoreys.Clear();
			PrjAllRelations.Clear();
			PrjAllEntitys.Clear();
			_allGeoMeshModels.Clear();
			_allGeoPointNormals.Clear();
			PorjectChanged.Invoke(this, null);
		}
		private void UpdataGeometryMeshModel()
		{
			var meshResult = new GeometryMeshResult();
			var allStoreys = PrjAllStoreys.Values.ToList();
			Parallel.ForEach(allStoreys, new ParallelOptions(), storey =>
			{
				int pIndex = -1;
				int gIndex = 0;
				var storeyGeoModels = new List<GeometryMeshModel>();
				var storeyGeoPointNormals = new List<PointNormal>();
				foreach (var item in storey.FloorEntityRelations)
				{
					var relation = item.Value;
					if (null == relation)
						continue;
					var entity = PrjAllEntitys[relation.RelationElementUid];
					if (null == entity || entity.AllShapeGeometries.Count < 1)
						continue;
					if (UnShowEntityTypes.Contains(entity.FriendlyTypeName))
						continue;
					var ptOffSet = storeyGeoPointNormals.Count();
					var material = THBimMaterial.GetTHBimEntityMaterial(entity.FriendlyTypeName, true);
					var meshModel = new GeometryMeshModel(gIndex, relation.Uid);
					meshModel.TriangleMaterial = material;
					foreach (var shapeGeo in entity.AllShapeGeometries)
					{
						if (shapeGeo == null || shapeGeo.ShapeGeometry == null || string.IsNullOrEmpty(shapeGeo.ShapeGeometry.ShapeData))
							continue;
						var ms = new MemoryStream((shapeGeo.ShapeGeometry as IXbimShapeGeometryData).ShapeData);
						var testData = ms.ToArray();
						var br = new BinaryReader(ms);
						var tr = br.ReadShapeTriangulation();
						if (tr.Faces.Count < 1)
							continue;
						var moveVector = shapeGeo.ShapeGeometry.TempOriginDisplacement;
						var transform = XbimMatrix3D.CreateTranslation(moveVector.X, moveVector.Y, moveVector.Z);
						transform = relation.Matrix3D * storey.Matrix3D * transform * shapeGeo.Matrix3D * Matrix3D;
						var allPts = tr.Vertices.ToArray();
						var allFace = tr.Faces;
						foreach (var face in allFace.ToList())
						{
							var ptIndexs = face.Indices.ToArray();
							for (int i = 0; i < face.TriangleCount; i++)
							{
								var triangle = new FaceTriangle();
								var pt1Index = ptIndexs[i * 3];
								var pt2Index = ptIndexs[i * 3 + 1];
								var pt3Index = ptIndexs[i * 3 + 2];
								var pt1 = allPts[pt1Index].TransPoint(transform);
								var pt1Normal = face.Normals.Last().Normal;
								if (pt1Index < face.Normals.Count())
									pt1Normal = face.Normals[pt1Index].Normal;
								pIndex += 1;
								triangle.ptIndex.Add(pIndex);
								storeyGeoPointNormals.Add(new PointNormal(pIndex, pt1, pt1Normal));
								var pt2 = allPts[pt2Index].TransPoint(transform);
								var pt2Normal = face.Normals.Last().Normal;
								if (pt2Index < face.Normals.Count())
									pt2Normal = face.Normals[pt2Index].Normal;
								pIndex += 1;
								triangle.ptIndex.Add(pIndex);
								storeyGeoPointNormals.Add(new PointNormal(pIndex, pt2, pt2Normal));
								var pt3 = allPts[pt3Index].TransPoint(transform);
								var pt3Normal = face.Normals.Last().Normal;
								if (pt3Index < face.Normals.Count())
									pt3Normal = face.Normals[pt3Index].Normal;
								pIndex += 1;
								triangle.ptIndex.Add(pIndex);
								storeyGeoPointNormals.Add(new PointNormal(pIndex, pt3, pt3Normal));
								meshModel.FaceTriangles.Add(triangle);
							}
						}
					}
					if (meshModel.FaceTriangles.Count < 1)
						continue;
					storeyGeoModels.Add(meshModel);
					gIndex += 1;
				}

				lock (meshResult)
				{
					int ptOffSet = meshResult.AllGeoPointNormals.Count;
					int gOffSet = meshResult.AllGeoModels.Count;
					foreach (var item in storeyGeoPointNormals)
					{
						item.PointIndex += ptOffSet;
					}
					foreach (var item in storeyGeoModels)
					{
						item.CIndex += gOffSet;
						foreach (var tr in item.FaceTriangles)
						{
							for (int i = 0; i < tr.ptIndex.Count; i++)
								tr.ptIndex[i] += ptOffSet;
						}
					}
					meshResult.AllGeoPointNormals.AddRange(storeyGeoPointNormals);
					meshResult.AllGeoModels.AddRange(storeyGeoModels);
				}
			});
			_allGeoMeshModels.Clear();
			_allGeoPointNormals.Clear();
			_allGeoPointNormals.AddRange(meshResult.AllGeoPointNormals);
			foreach (var item in meshResult.AllGeoModels)
			{
				_allGeoMeshModels.Add(item.EntityLable, item);
			}
		}
		private void UpdateCatchStoreyRelation()
		{
			if (!NeedReadEntityMesh && PrjAllStoreys.Count>0)
				return;
            PrjAllStoreys.Clear();
			PrjAllRelations.Clear();
			PrjAllEntitys.Clear();
			var storeys = this.ProjectAllStorey();
			foreach (var storey in storeys)
			{
				//In 1.4G 'wanda' model, two roof stories have same Uid
				if(!PrjAllStoreys.ContainsKey(storey.Uid))
					PrjAllStoreys.Add(storey.Uid, storey);
				foreach (var relation in storey.FloorEntityRelations)
				{
					if (PrjAllRelations.ContainsKey(relation.Key))
						continue;
					PrjAllRelations.Add(relation.Key, relation.Value);
				}
				foreach (var relation in storey.FloorEntitys)
				{
					if (PrjAllEntitys.ContainsKey(relation.Key))
						continue;
					PrjAllEntitys.Add(relation.Key, relation.Value);
				}
			}
		}
	}
}
