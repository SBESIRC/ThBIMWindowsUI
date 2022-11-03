using System;

namespace THBimEngine.Domain
{
    public class THBimBeam : THBimEntity, IEquatable<THBimBeam>
    {
        public THBimBeam(int id, string name, string material, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, material, geometryParam, describe, uid)
        {
            //
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public bool Equals(THBimBeam other)
        {
            throw new NotImplementedException();
        }
    }
}
