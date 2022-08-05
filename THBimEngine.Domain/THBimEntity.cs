using System.Collections.Generic;
using System;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public abstract class THBimEntity : THBimElement, IEquatable<THBimEntity>
    {
        /// <summary>
        /// 几何参数信息
        /// </summary>
        public GeometryParam GeometryParam { get; set; }
        /// <summary>
        /// 物体的Solid
        /// </summary>
        public List<IXbimSolid> EntitySolids { get; }
        /// <summary>
        /// 几何Mesh信息，Mesh中不包含物体的Solid
        /// </summary>
        public XbimShapeGeometry ShapeGeometry { get; set; }
        /// <summary>
        /// 物体开洞信息
        /// </summary>
        public List<THBimEntity> Openings { get; private set; }
        public THBimEntity(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            Openings = new List<THBimEntity>();
            GeometryParam = geometryParam;
            EntitySolids = new List<IXbimSolid>();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ GeometryParam.GetHashCode() ^ Openings.Count;
        }

        public bool Equals(THBimEntity other)
        {
            if (!base.Equals(other)) return false;
            if (Openings.Count != other.Openings.Count) return false;
            for (int i = 0; i < Openings.Count; i++)
            {
                if (!Openings[i].Equals(other.Openings[i]))
                {
                    return false;
                }
            }
            if (GeometryParam.Equals(other.GeometryParam))
            {
                return true;
            }
            return false;
        }
    }
}
