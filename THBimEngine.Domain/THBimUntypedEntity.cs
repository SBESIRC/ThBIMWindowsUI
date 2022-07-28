using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimUntypedEntity : THBimEntity, IEquatable<THBimUntypedEntity>
    {
        public Type OldType { get; set; }
        public string OldTypeName 
        { 
            get 
            {
                if (null != OldType)
                    return OldType.Name;
                return string.Empty;
            } 
        }
        public THBimUntypedEntity(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, geometryParam, describe, uid)
        {
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
