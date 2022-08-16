using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Common.XbimExtensions;

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
			AllEntitys =new Dictionary<string, THBimEntity>();
			AllGeoModels = new List<IfcMeshModel>();
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
		public List<IfcMeshModel> AllGeoModels { get; }
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
			AllEntitys.Clear();
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
		}
		public void UpdateCatchStoreyRelation() 
		{
			AllStoreys.Clear();
			AllRelations.Clear();
			foreach (var project in AllBimProjects)
			{
				foreach (var build in project.ProjectSite.SiteBuildings)
				{
					foreach (var storeyKeyValue in build.Value.BuildingStoreys)
					{
						AllStoreys.Add(storeyKeyValue.Key, storeyKeyValue.Value);
						foreach (var relation in storeyKeyValue.Value.FloorEntityRelations)
							AllRelations.Add(relation.Key, relation.Value);
					} 
				}
			}
		}
		public void ReadGeometryMesh() 
		{
			var meshResult = new GeoMeshResult();
			var allStoreys = AllStoreys.Values.ToList();
			Parallel.ForEach(allStoreys, new ParallelOptions(), storey =>
			{
				int pIndex = -1;
				int gIndex = 0;
				var storeyGeoModels = new List<IfcMeshModel>();
				var storeyGeoPointNormals = new List<PointNormal>();
				foreach (var item in storey.FloorEntityRelations)
				{
					var relation = item.Value;
					if (null == relation)
						continue;
					var entity = AllEntitys[relation.RelationElementUid];
					if (null == entity || entity.AllShapeGeometries.Count < 1)
						continue;
					if (UnShowEntityTypes.Contains(entity.FriendlyTypeName.ToString()))
						continue;
					
					var ptOffSet = storeyGeoPointNormals.Count();
					var material = THBimMaterial.GetTHBimEntityMaterial(entity.FriendlyTypeName, true);
					IfcMeshModel meshModel = new IfcMeshModel(gIndex, entity.Id);
					meshModel.RelationUid = relation.Uid;
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
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
			AllGeoPointNormals.AddRange(meshResult.AllGeoPointNormals);
			AllGeoModels.AddRange(meshResult.AllGeoModels);
			foreach (var item in meshResult.AllGeoModels) 
			{
				MeshEntiyRelationIndexs.Add(item.CIndex, item.RelationUid);
			}
		}
	}
	class GeoMeshResult
	{
		public List<IfcMeshModel> AllGeoModels { get; }
		public List<PointNormal> AllGeoPointNormals { get; }
		public GeoMeshResult()
		{
			AllGeoModels = new List<IfcMeshModel>();
			AllGeoPointNormals = new List<PointNormal>();
		}
	}
	public class PointNormal
	{
		public int PointIndex { get; set; }
		public PointVector Point { get; set; }
		public PointVector Normal { get; set; }
		public PointNormal(int pIndex, XbimPoint3D point, XbimVector3D normal)
		{
			PointIndex = pIndex;
			Point = new PointVector() { X = (float)point.X, Y = (float)point.Z, Z = (float)point.Y };
			Normal = new PointVector() { X = (float)normal.X, Y = (float)normal.Z, Z = (float)normal.Y };
		}

	}
	public class IfcMeshModel
	{
		public int CIndex { get; set; }
		public int IfcIndex { get; }
		public string RelationUid { get; set; }
		public THBimMaterial TriangleMaterial { get; set; }
		public List<FaceTriangle> FaceTriangles { get; }
		public IfcMeshModel(int index, int ifcIndex)
		{
			CIndex = index;
			IfcIndex = ifcIndex;
			FaceTriangles = new List<FaceTriangle>();
		}
	}
	public class FaceTriangle
	{
		public List<int> ptIndex { get; }
		public FaceTriangle()
		{
			ptIndex = new List<int>();
		}
	}

	public class PointVector
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
	}
}
