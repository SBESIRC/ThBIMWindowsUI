using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class GeometryMeshModel : ICloneable
	{
		public int CIndex { get; set; }
		public int IfcIndex { get; }
		public string RelationUid { get; set; }
		public THBimMaterial TriangleMaterial { get; set; }
		public List<FaceTriangle> FaceTriangles { get; }
		public GeometryMeshModel(int index, int ifcIndex)
		{
			CIndex = index;
			IfcIndex = ifcIndex;
			FaceTriangles = new List<FaceTriangle>();
		}

		public object Clone()
		{
			var model = new GeometryMeshModel(this.CIndex, this.IfcIndex);
			model.TriangleMaterial = this.TriangleMaterial;
			model.RelationUid = this.RelationUid;
			foreach (var item in this.FaceTriangles)
			{
				var cloneFTri = item.Clone() as FaceTriangle;
				model.FaceTriangles.Add(cloneFTri);
			}
			return model;
		}
	}
}
