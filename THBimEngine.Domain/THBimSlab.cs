using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimSlab : THBimEntity, IEquatable<THBimSlab>
    {
        /// <summary>
        /// 降板信息说明
        /// Outline,降板轮廓
        /// ZAxisLength -- 板厚
        /// ZOffSet降板高度
        /// YAxisLength -- 降板外扩宽度
        /// </summary>
        public List<GeometryStretch> SlabDescendingDatas { get; }
        public THBimSlab(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name,geometryParam, describe, uid)
        {
            SlabDescendingDatas = new List<GeometryStretch>();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(THBimSlab other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
