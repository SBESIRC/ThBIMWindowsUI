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
        public Dictionary<int, THBimElementRelation> FloorEntitys { get; private set; }
        /// <summary>
        /// 楼层原点
        /// </summary>
        public XbimPoint3D Origin { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string MemoryStoreyId { get; set; }
        public XbimMatrix3D MemoryMatrix3d { get; set; }
        public THBimStorey(int id, string name,double elevation,double levelHeight, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            FloorEntitys = new Dictionary<int, THBimElementRelation>();
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
            for(int i =0; i < FloorEntitys.Count;i++)
            {
                if (!FloorEntitys[i].Equals(other.FloorEntitys[i]))
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
    }
}
