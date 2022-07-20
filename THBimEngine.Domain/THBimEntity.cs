using System.Collections.Generic;
using System;
namespace THBimEngine.Domain
{
    public abstract class THBimEntity : THBimElement, IEquatable<THBimEntity>
    {
        public GeometryParam GeometryParam { get; set; }
        public List<THBimOpening> Openings { get; private set; }
        public THBimEntity(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id,name,describe,uid)
        {
            Openings = new List<THBimOpening>();
            GeometryParam = geometryParam;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ GeometryParam.GetHashCode() ^ Openings.Count;
        }

        public bool Equals(THBimEntity other)
        {
            if (!base.Equals(other)) return false;
            if (Openings.Count != other.Openings.Count) return false;
            for(int i =0; i < Openings.Count;i++)
            {
                if (!Openings[i].Equals(other.Openings[i]))
                {
                    return false;
                }
            }
            if(GeometryParam.Equals(other.GeometryParam) &&
                Openings.Equals(other.Openings))
            {
                return true;
            }
            return false;
        }
    }
}
