using System;

namespace THBimEngine.Domain
{
    public class THBimWindow : THBimEntity, IEquatable<THBimOpening>
    {
        public THBimWindow(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, geometryParam, describe, uid)
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
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
