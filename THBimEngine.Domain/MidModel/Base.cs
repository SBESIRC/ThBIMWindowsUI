using System.Collections.Generic;
using System.IO;

namespace THBimEngine.Domain.MidModel
{
    public struct Color// 颜色信息rgba
    { 
		public float r, g, b, a;

		public Color(float _r, float _g, float _b, float _a)
        {
            r = _r;
            g = _g;
            b = _b;
            a = _a;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(r);
            writer.Write(g);
            writer.Write(b);
            writer.Write(a);
        }
	};

    public struct Vec3
    {
        public float x, y, z;
        public Vec3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public Vec3(PointVector pt)
        {
            x = pt.X;
            y = pt.Y;
            z = pt.Z;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    };
}
