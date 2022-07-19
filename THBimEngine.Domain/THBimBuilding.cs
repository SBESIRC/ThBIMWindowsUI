using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class THBimBuilding : THBimElement
    {
        public List<THBimStorey> BuildingStoreys { get; }
        public THBimBuilding(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            BuildingStoreys = new List<THBimStorey>();
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
