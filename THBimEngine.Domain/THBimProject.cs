using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;

namespace THBimEngine.Domain
{
    public class THBimProject : THBimElement,IEquatable<THBimProject>
    {
        public string ProjectIdentity { get; set; }
        public BuildingCatagory Catagory { get; set; }
        public THBimSite ProjectSite { get; set; }
        public object SourceProject { get; set; }
		public bool NeedCreateMesh { get; set; }
		public bool HaveChange { get; set; }
		private Dictionary<string,GeometryMeshModel> _allGeoMeshModels { get; }
		private List<PointNormal> _allGeoPointNormals { get; }
		public Dictionary<string,THBimEntity> PrjAllEntitys { get; }
		public Dictionary<string,THBimElementRelation> PrjAllRelations { get; }
        public Dictionary<string, THBimStorey> PrjAllStoreys { get; }
		public List<string> UnShowEntityTypes { get; }
		public THBimProject(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
			HaveChange = false;
			NeedCreateMesh = true;
			_allGeoMeshModels = new Dictionary<string, GeometryMeshModel>();
			_allGeoPointNormals = new List<PointNormal>();
			PrjAllEntitys = new Dictionary<string, THBimEntity>();
			PrjAllRelations = new Dictionary<string, THBimElementRelation>();
			PrjAllStoreys = new Dictionary<string, THBimStorey>();
			UnShowEntityTypes = new List<string>();
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

        public void UpdataGeometryMeshModel() 
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
						transform = relation.Matrix3D * storey.Matrix3D * transform * shapeGeo.Matrix3D;
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
        public Dictionary<string,GeometryMeshModel> AllGeoModels()
        {
			var resList = new Dictionary<string,GeometryMeshModel>();
			foreach (var item in _allGeoMeshModels) 
			{
				resList.Add(item.Key,item.Value.Clone() as GeometryMeshModel);
			}
			return resList;
		}
        public List<PointNormal> AllGeoPointNormals(bool yzExchange = false)
        {
			var resList = new List<PointNormal>();
			foreach (var item in _allGeoPointNormals) 
			{
				var clonedPt = item.Clone() as PointNormal;
				if(yzExchange)
                {
					float y = clonedPt.Point.Y;
					float z = clonedPt.Point.Z;
					clonedPt.Point.Y = z;
					clonedPt.Point.Z = y;
				}
				resList.Add(clonedPt);
			}
			return resList;
        }
		/// <summary>
		/// 临时使用，后续删除
		/// </summary>
		/// <param name="meshModels"></param>
		/// <param name="pointNormals"></param>
		public void AddGeoMeshModels(List<GeometryMeshModel> meshModels, List<PointNormal> pointNormals) 
		{
			_allGeoMeshModels.Clear();
			_allGeoPointNormals.Clear();
			foreach (var item in meshModels)
			{
				_allGeoMeshModels.Add(item.EntityLable,item);
			}
			_allGeoPointNormals.AddRange(pointNormals);
		}
		public void ClearAllData()
		{
			PrjAllStoreys.Clear();
			PrjAllRelations.Clear();
			PrjAllEntitys.Clear();
			_allGeoMeshModels.Clear();
			_allGeoPointNormals.Clear();
		}
		public void UpdateCatchStoreyRelation()
		{
			if (!NeedCreateMesh && PrjAllStoreys.Count>0)
				return;
            PrjAllStoreys.Clear();
			PrjAllRelations.Clear();
			PrjAllEntitys.Clear();
			var storeys = this.ProjectAllStorey();
			foreach (var storey in storeys)
			{
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
