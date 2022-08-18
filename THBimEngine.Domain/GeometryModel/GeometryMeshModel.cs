using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class GeometryMeshModel : ICloneable
	{
		public int CIndex { get; set; }
		public string EntityLable { get; }
		public THBimMaterial TriangleMaterial { get; set; }
		public List<FaceTriangle> FaceTriangles { get; }
		public GeometryMeshModel(int index, string ifcIndex)
		{
			CIndex = index;
			EntityLable = ifcIndex;
			FaceTriangles = new List<FaceTriangle>();
		}

		public object Clone()
		{
			var model = new GeometryMeshModel(this.CIndex, this.EntityLable);
			model.TriangleMaterial = this.TriangleMaterial;
			foreach (var item in this.FaceTriangles)
			{
				var cloneFTri = item.Clone() as FaceTriangle;
				model.FaceTriangles.Add(cloneFTri);
			}
			return model;
		}
	}
}
