using System;
using System.Collections.Generic;

namespace THBimEngine.Domain.GeometryModel
{
	public class GeometryFaceModel : ICloneable
	{
		public List<FaceTriangle> faceTriangles { get; }
		public GeometryFaceModel()
		{
			faceTriangles = new List<FaceTriangle>();
		}

		public object Clone()
		{
			var clone = new GeometryFaceModel();
			foreach (var item in faceTriangles)
			{
				clone.faceTriangles.Add(item);
			}
			return clone;
		}
	}
}
