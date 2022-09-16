using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimOpening : THBimEntity,IEquatable<THBimOpening>
    {
        public THBimOpening(int id, string name, string material,GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, material,geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(THBimOpening other)
        {
            if(!base.Equals(other)) return false;
            return true;
        }
    }
}
