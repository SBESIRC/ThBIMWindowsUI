using System;
using System.Collections.Generic;
using THBimEngine.Domain.GeometryModel;

namespace THBimEngine.Domain
{
	public class GeometryMeshModelForSUProject : ICloneable
	{
		public int CIndex { get; set; }
		public string EntityLable { get; }
		public List<GeometryFaceModel> Faces { get; }
		public GeometryMeshModelForSUProject(int index, string ifcIndex)
		{
			CIndex = index;
			EntityLable = ifcIndex;
			Faces = new List<GeometryFaceModel>();
		}

		public object Clone()
		{
			var model = new GeometryMeshModelForSUProject(this.CIndex, this.EntityLable);
			foreach (var item in this.Faces)
			{
				var cloneFTri = item.Clone() as GeometryFaceModel;
				model.Faces.Add(cloneFTri);
			}
			return model;
		}
	}
}

