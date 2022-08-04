using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;

namespace THBimEngine.Domain
{
    public class THBimIFCEntity : THBimEntity, IEquatable<THBimIFCEntity>
    {
        public IPersistEntity IfcEntity { get; }
        public THBimIFCEntity(IPersistEntity entity) : this(entity.EntityLabel, entity.ExpressType.ExpressName, null) 
        {
            IfcEntity = entity;
        }
        public THBimIFCEntity(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public bool Equals(THBimIFCEntity other)
        {
            throw new NotImplementedException();
        }
    }
}
