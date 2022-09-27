using System;

namespace THBimEngine.Domain
{
    public struct PointVector
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }

		public PointVector Clone()
		{
			return new PointVector()
			{
				X = this.X,
				Y = this.Y,
				Z = this.Z,
			};
		}
	}
}
