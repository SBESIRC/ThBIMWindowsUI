using System;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public class PointNormal
	{
		public int PointIndex { get; set; }
		public PointVector Point { get; set; }
		public PointVector Normal { get; set; }
		public PointNormal(int pIndex, XbimPoint3D point, XbimVector3D normal)
		{
			PointIndex = pIndex;
			Point = new PointVector() 
			{ 
				X = (float)point.X, 
				Y = (float)point.Z, 
				Z = (float)point.Y 
			};
			Normal = new PointVector() 
			{ 
				X = (float)normal.X,
				Y = (float)normal.Z, 
				Z = (float)normal.Y 
			};
		}
		public PointNormal GetRealData() 
		{
			var realData = this.Clone();
			realData.Point = this.Point;
			realData.Normal = this.Normal;

			return realData;
		}
		public PointNormal Clone()
		{
			var clone = new PointNormal(this.PointIndex, XbimPoint3D.Zero, XbimVector3D.Zero);
			clone.Point = this.Point;
			clone.Normal = this.Normal;
			return clone;
		}
	}
}
