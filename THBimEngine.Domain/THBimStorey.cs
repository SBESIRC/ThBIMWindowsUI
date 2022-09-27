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
        public Dictionary<string, THBimElementRelation> FloorEntityRelations { get; private set; }
        public Dictionary<string, THBimEntity> FloorEntitys { get; private set; }
        /// <summary>
        /// 楼层原点
        /// </summary>
        public XbimPoint3D Origin { get; set; }
        /// <summary>
        /// 链接楼层ID，标准层第一层或非标层为 NULL
        /// </summary>
        public string MemoryStoreyId { get; set; }
        public XbimMatrix3D MemoryMatrix3d { get; set; }

        /// <summary>
        /// 轴网数据
        /// </summary>
        public ThGridLineSyetemData GridLineSyetemData { get; set; }

        public THBimStorey(int id, string name,double elevation,double levelHeight, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            FloorEntityRelations = new Dictionary<string, THBimElementRelation>();
            FloorEntitys = new Dictionary<string, THBimEntity>();
            Elevation = elevation;
            LevelHeight = levelHeight;
            MemoryStoreyId = string.Empty;
        }

        public Dictionary<string, List<THBimEntity>> GetTypeGroupValue() 
        {
            Dictionary<string, List<THBimEntity>> typeGroup = new Dictionary<string, List<THBimEntity>>();
            if (FloorEntityRelations.Count < 1)
                return typeGroup;
            foreach (var item in FloorEntityRelations.GroupBy(c => c.GetType())) 
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
                ^ MemoryStoreyId.GetHashCode() ^ FloorEntityRelations.Count;
        }

        public bool Equals(THBimStorey other)
        {
            if (!base.Equals(other)) return false;
            if(FloorEntityRelations.Count != other.FloorEntityRelations.Count)
            {
                return false;
            }
            foreach(var key in FloorEntityRelations.Keys)
            {
                if (!other.FloorEntityRelations.ContainsKey(key))
                {
                    return false;
                }
                //has problem
                if (!FloorEntityRelations[key].Equals(other.FloorEntityRelations[key]))
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
            var newStoreyUids = newStorey.FloorEntityRelations.Keys;
            var storeyUids = FloorEntityRelations.Keys;
            return newStoreyUids.Except(storeyUids).ToList();
        }
        public List<string> GetRemovedComponentUids(THBimStorey newStorey)
        {
            var newStoreyUids = newStorey.FloorEntityRelations.Keys;
            var storeyUids = FloorEntityRelations.Keys;
            return storeyUids.Except(newStoreyUids).ToList();
        }

        public List<string> GetUpdatedComponentUids(THBimStorey newStorey)
        {
            var newUpdatedUids = new List<string>();
            var entityIds = this.FloorEntitys.Keys;
            var newEntityIds = newStorey.FloorEntitys.Keys;
            var checkEntityIds = entityIds.Intersect(newEntityIds).ToList();
            if (checkEntityIds.Count > 0)
            {
                foreach (var id in checkEntityIds) 
                {
                    var oldEntity = this.FloorEntitys[id];
                    var newEntity = newStorey.FloorEntitys[id];
                    if (oldEntity == null || newEntity == null)
                        continue;
                    if (!newEntity.Equals(oldEntity))
                    {
                        newUpdatedUids.Add(id);
                    }
                }
            }
            else 
            {
                var newStoreyUids = newStorey.FloorEntityRelations.Keys;
                var storeyUids = FloorEntityRelations.Keys;
                var unionUids = newStoreyUids.Intersect(storeyUids);
                foreach (var uid in unionUids)
                {
                    if (string.IsNullOrEmpty(uid))
                        continue;
                    var oldValue = FloorEntityRelations[uid];
                    var newValue = newStorey.FloorEntityRelations[uid];
                    if (oldValue == null || newValue == null)
                        continue;
                    if (!oldValue.Equals(newValue))
                    {
                        newUpdatedUids.Add(uid);
                    }
                }
            }
            return newUpdatedUids;
        }
    }
}
