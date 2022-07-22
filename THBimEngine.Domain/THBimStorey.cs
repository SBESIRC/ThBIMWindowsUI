using System;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    /// <summary>
    /// 楼层信息
    /// </summary>
    public class THBimStorey : THBimElement, IEquatable<THBimStorey>
    {
        /// <summary>
        /// 楼层标高
        /// </summary>
        public double Elevation { get; set; }
        /// <summary>
        /// 层高
        /// </summary>
        public double LevelHeight { get; set; }
        /// <summary>
        /// 该楼层元素
        /// </summary>
        public Dictionary<string, THBimElementRelation> FloorEntitys { get; private set; }
        /// <summary>
        /// 楼层原点
        /// </summary>
        public XbimPoint3D Origin { get; set; }
        /// <summary>
        /// 链接楼层ID，标准层第一层或非标层为 NULL
        /// </summary>
        public string MemoryStoreyId { get; set; }
        public XbimMatrix3D MemoryMatrix3d { get; set; }
        public THBimStorey(int id, string name,double elevation,double levelHeight, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            FloorEntitys = new Dictionary<string, THBimElementRelation>();
            Elevation = elevation;
            LevelHeight = levelHeight;
            MemoryStoreyId = string.Empty;
        }

        public Dictionary<string, List<THBimEntity>> GetTypeGroupValue() 
        {
            Dictionary<string, List<THBimEntity>> typeGroup = new Dictionary<string, List<THBimEntity>>();
            if (FloorEntitys.Count < 1)
                return typeGroup;
            foreach (var item in FloorEntitys.GroupBy(c => c.GetType())) 
            {
                //typeGroup.Add(item.)
            }
            return typeGroup;
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Elevation.GetHashCode() ^ LevelHeight.GetHashCode()^ Origin.GetHashCode()
                ^ MemoryStoreyId.GetHashCode() ^ FloorEntitys.Count;
        }

        public bool Equals(THBimStorey other)
        {
            if (!base.Equals(other)) return false;
            if(FloorEntitys.Count!=other.FloorEntitys.Count)
            {
                return false;
            }
            foreach(var key in FloorEntitys.Keys)
            {
                if (!other.FloorEntitys.ContainsKey(key))
                {
                    return false;
                }
                if (!FloorEntitys[key].Equals(other.FloorEntitys[key]))
                {
                    return false;
                }
            }
            if(Elevation.FloatEquals(other.Elevation)&&
               LevelHeight.FloatEquals(other.LevelHeight) &&
               Origin.Equals(other.Origin) &&
               MemoryStoreyId.Equals(other.MemoryStoreyId))
            {
                return true;
            }
            return false;
        }

        public List<string> GetAddedComponentUids(THBimStorey newStorey)
        {
            var newStoreyUids = newStorey.FloorEntitys.Keys.ToList();
            var storeyUids = FloorEntitys.Keys.ToList();
            return newStoreyUids.Except(storeyUids).ToList();
        }
        public List<string> GetRemovedComponentUids(THBimStorey newStorey)
        {
            var newStoreyUids = newStorey.FloorEntitys.Keys.ToList();
            var storeyUids = FloorEntitys.Keys.ToList();
            return storeyUids.Except(newStoreyUids).ToList();
        }

        public List<string> GetUpdatedComponentUids(THBimStorey newStorey)
        {
            var newStoreyUids = newStorey.FloorEntitys.Keys.ToList();
            var storeyUids = FloorEntitys.Keys.ToList();
            var unionUids = newStoreyUids.Intersect(storeyUids).ToList();
            var newUpdatedUids = new List<string>();
            foreach (var uid in unionUids)
            {
                if (!FloorEntitys[uid].Equals(newStorey.FloorEntitys[uid]))
                {
                    newUpdatedUids.Add(uid);
                }
            }
            return newUpdatedUids;
        }
    }
}
