using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public abstract class THBimEntity : THBimElement
    {
        public GeometryParam GeometryParam { get; set; }
        public List<THBimOpening> Openings { get; private set; }
        public THBimEntity(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id,name,describe,uid)
        {
            Openings = new List<THBimOpening>();
            GeometryParam = geometryParam;
        }
    }
}
