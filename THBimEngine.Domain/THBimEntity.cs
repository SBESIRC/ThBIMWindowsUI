using System;
using System.Collections.Generic;
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
        /// 几何Mesh信息，Mesh中不包含物体的Solid (一个物体可能包含多个实体信息)
        /// </summary>
        public List<THBimShapeGeometry> AllShapeGeometries { get; }
        /// <summary>
        /// 物体开洞信息
        /// </summary>
        public List<THBimOpening> Openings { get; private set; }
        /// <summary>
        /// 材质
        /// </summary>
        public string Material { get; set; }

        public THBimEntity(int id, string name, string material, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            Openings = new List<THBimOpening>();
            GeometryParam = geometryParam;
            EntitySolids = new List<IXbimSolid>();
            AllShapeGeometries = new List<THBimShapeGeometry>();
            Material = material;
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

    public class THBimShapeGeometry
    {
        public XbimShapeGeometry ShapeGeometry { get; }
        public XbimMatrix3D Matrix3D { get; set; }
        public THBimShapeGeometry(XbimShapeGeometry shapeGeometry)
        {
            ShapeGeometry = shapeGeometry;
            Matrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
        }
        public THBimShapeGeometry(XbimShapeGeometry shapeGeometry, XbimMatrix3D matrix3D)
        {
            Matrix3D = matrix3D;
            ShapeGeometry = shapeGeometry;
        }
    }
}
