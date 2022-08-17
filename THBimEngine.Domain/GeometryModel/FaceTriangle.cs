using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class FaceTriangle : ICloneable
	{
		public List<int> ptIndex { get; }
		public FaceTriangle()
		{
			ptIndex = new List<int>();
		}

		public object Clone()
		{
			var clone = new FaceTriangle();
			foreach (var item in ptIndex)
			{
				clone.ptIndex.Add(item);
			}
			return clone;
		}
	}
}
