using System;
using System.Collections.Generic;
using System.IO;

namespace THBimEngine.Domain.MidModel
{
    public class OutingPolygon
    {
        public int group_id;   //	构件id
        public int type_id;    //	类型id
        public int id;         //	自身id
        public List<int> ptsIndex = new List<int>();

        public OutingPolygon(FaceTriangle triangle, List<PointNormal> allPoints, ref int triangleIndex,
            UniComponent uniComponent, ref int ptIndex, List<Vec3> Points,bool firstTriangles)
        {
            id = triangleIndex;
            triangleIndex++;
            group_id = uniComponent.unique_id;
            type_id = uniComponent.type_id;
            var cnt = triangle.ptIndex.Count;
            for (int i = 0; i < cnt; i++)
            {
                var pt = allPoints[triangle.ptIndex[i]].Point;
                if(firstTriangles&&i==0)//第一个三角面片的第一个点
                {
                    GetBbx(pt, uniComponent);
                }
                else
                {
                    UpdateBbx(pt, uniComponent);
                }
                Points.Add(new Vec3(pt));
                ptsIndex.Add(ptIndex);
                ptIndex++;
            }
        }

        public void GetBbx(PointVector pt, UniComponent uniComponent)
        {
            uniComponent.x_l = pt.X;
            uniComponent.x_r = pt.X;
            uniComponent.y_l = pt.Y;
            uniComponent.y_r = pt.Y;
            uniComponent.z_l = pt.Z;
            uniComponent.z_r = pt.Z;
        }

        public void UpdateBbx(PointVector pt,UniComponent uniComponent)
        {
            uniComponent.x_l = Math.Min(uniComponent.x_l, pt.X);
            uniComponent.x_r = Math.Max(uniComponent.x_r, pt.X);
            uniComponent.y_l = Math.Min(uniComponent.y_l, pt.Y);
            uniComponent.y_r = Math.Max(uniComponent.y_r, pt.Y);
            uniComponent.z_l = Math.Min(uniComponent.z_l, pt.Z);
            uniComponent.z_r = Math.Max(uniComponent.z_r, pt.Z);
        }

        public void WriteToFile(BinaryWriter writer,List<Vec3> points)
        {
            writer.Write(type_id);
            writer.Write(group_id);
            writer.Write(id);
            foreach (var index in ptsIndex)
            {
                var pt = points[index];
                pt.Write(writer);
            }
        }
    }
}
