using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class THBimBuilding : THBimElement,IEquatable<THBimBuilding>
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
        public override int GetHashCode()
        {
            return base.GetHashCode() ^ BuildingStoreys.Count;
        }

        public bool Equals(THBimBuilding other)
        {
            if (!base.Equals(other)) return false;
            if (BuildingStoreys.Count != other.BuildingStoreys.Count) return false;
            for(int i =0; i < BuildingStoreys.Count;i++)
            {
                if (!BuildingStoreys[i].Equals(other.BuildingStoreys[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
