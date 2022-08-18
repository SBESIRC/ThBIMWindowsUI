using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain.MidModel
{
    public class OutingPolygon// 存储组、类型、自身id（索引）以及拥有的顶点位置（法向量）信息（一个三角面片上的3个点）
    {

		public int group_id;   //	构件id
		public int type_id;    //	类型id
		public int id;         //	自身id
		//public bool clip_flag; //	径向三角片，已无用

		public List<int> ptsIndex;   // 顶点位置数组（vec3）某个三角面片的顶点(vec3)信息（一般长度为3）

		public OutingPolygon(FaceTriangle triangle, List<PointNormal> allPoints, ref int triangleIndex, 
			int parentId, ref int ptIndex,List<vec3> Points)
        {
			id = triangleIndex;
			triangleIndex++;
			group_id = parentId;

			var cnt = triangle.ptIndex.Count;
			for(int i =0;i<cnt-1;i++)
            {
				var pt = allPoints[triangle.ptIndex[i]].Point;
				Points.Add(new vec3(pt));
				ptsIndex.Add(ptIndex);
				ptIndex++;
			}
		}
	}
}
