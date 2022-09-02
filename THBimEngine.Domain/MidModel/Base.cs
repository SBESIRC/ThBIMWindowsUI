using System;
using System.Collections.Generic;
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

        public Vec3 ReverseX()
        {
            return new Vec3(-x,-y,-z);
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

        /// <summary>
        /// 删除字符串中的中文
        /// </summary>
        public static string DeleteChinese(string str)
        {
            string retValue = str;
            if (System.Text.RegularExpressions.Regex.IsMatch(str, @"[\u4e00-\u9fa5]"))
            {
                retValue = string.Empty;
                var strsStrings = str.ToCharArray();
                for (int index = 0; index < strsStrings.Length; index++)
                {
                    if (strsStrings[index] >= 0x4e00 && strsStrings[index] <= 0x9fa5)
                    {
                        continue;
                    }
                    retValue += strsStrings[index];
                }
            }
            return retValue;
        }

        public static void WriteStr(this string str, BinaryWriter writer)
        {
            var buffer = Encoding.GetEncoding("utf-8").GetBytes(str);
            writer.Write((ulong)buffer.Length);
            writer.Write(buffer);
        }
    }
}
