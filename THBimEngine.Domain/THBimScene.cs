using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class THBimScene
    {
		public string DocumentId { get; }
		public THBimScene(string id) 
		{
			DocumentId = id;
			AllGeoModels = new List<GeometryMeshModel>();
			AllGeoPointNormals = new List<PointNormal>();
			MeshEntiyRelationIndexs = new Dictionary<int, MeshEntityIdentifier>();
		}
		/// <summary>
		/// 所有物体的三角面片信息集合
		/// </summary>
		public List<GeometryMeshModel> AllGeoModels { get; }
		/// <summary>
		/// 所有物体的顶点集合
		/// </summary>
		public List<PointNormal> AllGeoPointNormals { get; }
		public Dictionary<int, MeshEntityIdentifier> MeshEntiyRelationIndexs { get; }//relationId
		
		public void ClearAllData() 
		{
			AllGeoModels.Clear();
			AllGeoPointNormals.Clear();
			MeshEntiyRelationIndexs.Clear();
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

	public class MeshEntityIdentifier 
	{
		public int GlobalMeshIndex { get;  }
		public string ProjectId { get;  }
		public string ProjectEntityId { get; }
		public MeshEntityIdentifier(int meshGId,string prjectId,string entityId) 
		{
			GlobalMeshIndex = meshGId;
			ProjectId = prjectId;
			ProjectEntityId = entityId;
		}
	}
}
