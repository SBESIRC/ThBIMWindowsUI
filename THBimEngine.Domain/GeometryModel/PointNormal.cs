using System;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public class PointNormal : ICloneable
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
			var realData = this.Clone() as PointNormal;
			realData.Point = new PointVector() 
			{ 
				X = this.Point.X,
				Y = this.Point.Z, 
				Z = this.Point.Y 
			};
			realData.Normal = new PointVector() 
			{ 
				X = this.Normal.X,
				Y = this.Normal.Z,
				Z = this.Normal.Y 
			};
			return realData;
		}
		public object Clone()
		{
			var clone = new PointNormal(this.PointIndex, XbimPoint3D.Zero, XbimVector3D.Zero);
			clone.Point = this.Point.Clone() as PointVector;
			clone.Normal = this.Normal.Clone() as PointVector;
			return clone;
		}
	}
}
