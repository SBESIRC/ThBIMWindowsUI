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

    public struct vec3
    {
        public float x, y, z;
        public vec3(float _x, float _y, float _z)
        {
            x = _x;
            y = _y;
            z = _z;
        }

        public vec3(PointVector pt)
        {
            x = (float)pt.X;
            y = (float)pt.Y;
            z = (float)pt.Z;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
    };

    public class Compute
    {
        public vec3 cross(vec3 a, vec3 b)
        {
            return new vec3(a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x);
        }
    }

    public class InputEntity
    {
        public int Id;
        public List<FaceTriangle> triangles;
        public List<int> ptsIndex;

        public InputEntity(int id, string uid, Dictionary<string, GeometryMeshModel> allGeoMeshModels)
        {
            Id = id;
            triangles = allGeoMeshModels[uid].FaceTriangles;
        }
    }
}
