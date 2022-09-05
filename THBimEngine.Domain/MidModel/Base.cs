using System;
using System.IO;
using System.Text;

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

        public Vec3 Dir(Vec3 vec3)
        {
            return new Vec3(x-vec3.x,y-vec3.y,z-vec3.z);
        }

        public bool Equals(Vec3 vec3)
        {
            var dot = x * vec3.x + y * vec3.y + z * vec3.z;
            var cos = Math.Abs(dot / (this.Norm()*vec3.Norm()));
            return cos > Math.Cos(0.017);
        }

        public double Norm()
        {
            return Math.Sqrt(x * x + y * y + z * z);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    };

    public struct FloorPara
    {
        public int Num;
        public int StdNum;
        public double Height;

        public FloorPara(int num, int stdNum, double height)
        {
            Num = num;
            StdNum = stdNum;
            Height = height;
        }
    }

    public static class Method
    {
        public static void WriteStr(this string str, BinaryWriter writer)
        {
            var buffer = Encoding.GetEncoding("utf-8").GetBytes(str);
            writer.Write((ulong)buffer.Length);
            writer.Write(buffer);
        }
    }
}
