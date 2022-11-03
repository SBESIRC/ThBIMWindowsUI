using System;

namespace THBimEngine.Domain
{
    public class THBimColumn : THBimEntity, IEquatable<THBimColumn>
    {
        public THBimColumn(int id, string name, string material, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, material, geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public bool Equals(THBimColumn other)
        {
            throw new NotImplementedException();
        }
    }
}
