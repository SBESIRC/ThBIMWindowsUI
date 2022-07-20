using System;

namespace THBimEngine.Domain
{
    public class THBimDoor : THBimEntity,IEquatable<THBimDoor>
    {
        public THBimDoor(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, geometryParam, describe, uid)
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
        public bool Equals(THBimDoor other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
