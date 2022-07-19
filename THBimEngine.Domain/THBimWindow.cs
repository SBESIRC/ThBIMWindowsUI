using System;

namespace THBimEngine.Domain
{
    public class THBimWindow : THBimEntity
    {
        public THBimWindow(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
