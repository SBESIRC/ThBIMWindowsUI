using System;

namespace THBimEngine.Domain
{
    public class PointVector : ICloneable
	{
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }

		public object Clone()
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
