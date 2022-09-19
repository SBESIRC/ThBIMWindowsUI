using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimUntypedEntity : THBimEntity, IEquatable<THBimUntypedEntity>
    {
        public string EntityTypeName { get; set; }
        public THBimUntypedEntity(int id, string name,string material, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, material,geometryParam, describe, uid)
        {
            EntityTypeName = "";
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public bool Equals(THBimUntypedEntity other)
        {
            throw new NotImplementedException();
        }
    }
}
