using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain.MidModel
{
    public class Edge// 存储边信息 通常只有两个点
    {
        public int group_id;   // 构件id
        public int type_id;    // 类型id
        public int Id;         // 自身id
        public List<int> points; // 记录边上顶点的位置（通常只有两个点）
        public Edge(ref int edgeIndex, int parentId, int index1,int index2)
        {
            Id = edgeIndex;
            edgeIndex++;
            group_id = parentId;
            points.Add(index1);
            points.Add(index2);
        }
    }
}
