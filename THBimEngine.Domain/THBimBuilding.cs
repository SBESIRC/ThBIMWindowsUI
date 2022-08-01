using System;
using System.Collections.Generic;
using System.Linq;

namespace THBimEngine.Domain
{
    public class THBimBuilding : THBimElement,IEquatable<THBimBuilding>
    {
        public Dictionary<string, THBimStorey> BuildingStoreys { get; }
        public THBimBuilding(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            BuildingStoreys = new Dictionary<string, THBimStorey>();
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
            foreach(var key in BuildingStoreys.Keys)
            {
                if(other.BuildingStoreys.ContainsKey(key))
                {
                    return false;
                }
                if (!BuildingStoreys[key].Equals(other.BuildingStoreys[key]))
                {
                    return false;
                }
            }
            return true;
        }

        public List<string> GetNewlyAddedComponentUids(THBimBuilding newBuilding,List<string> newStoreyUids)
        {
            var addedComponentUids = new List<string>();
            foreach (var uid in newStoreyUids)
            {
                var newStorey = newBuilding.BuildingStoreys[uid];
                if (BuildingStoreys.ContainsKey(uid))
                {
                    var storey = BuildingStoreys[uid];
                    addedComponentUids.AddRange(storey.GetAddedComponentUids(newStorey));
                }
                else
                {
                    addedComponentUids.AddRange(newStorey.FloorEntityRelations.Keys.ToList());
                }
                
            }
            return addedComponentUids;
        }

        public List<string> GetRemovedComponentUids(THBimBuilding newBuilding, List<string> storeyUids)
        {
            var removedComponentUids = new List<string>();
            foreach (var uid in storeyUids)
            {
                var storey = BuildingStoreys[uid];
                if (newBuilding.BuildingStoreys.ContainsKey(uid))
                {
                    var newStorey = newBuilding.BuildingStoreys[uid];
                    removedComponentUids.AddRange(storey.GetRemovedComponentUids(newStorey));
                }
                else
                {
                    removedComponentUids.AddRange(storey.FloorEntityRelations.Keys.ToList());
                }
            }
            return removedComponentUids;
        }

        public List<string> GetUpdatedComponentUids(THBimBuilding newBuilding,List<string> unionUids)
        {
            var unionComponentUids = new List<string>();
            foreach (var uid in unionUids)
            {
                var storey = BuildingStoreys[uid];
                var newStorey = newBuilding.BuildingStoreys[uid];
                unionComponentUids.AddRange(storey.GetUpdatedComponentUids(newStorey));
            }
            return unionComponentUids;
        }
    }
}
