using System;

namespace THBimEngine.Domain
{
    public class THBimDoor : THBimEntity, IEquatable<THBimDoor>
    {
        /// <summary>
        /// 门开启方向
        /// </summary>
        public uint Swing { get; set; }
        /// <summary>
        /// 门类型
        /// </summary>
        public uint OperationType { get; set; }

        public THBimDoor(int id, string name,string material, GeometryParam geometryParam, uint swing, uint operationType, string describe = "", string uid = "") : base(id, name, material, geometryParam, describe, uid)
        {
            Swing = swing;
            OperationType = operationType;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(THBimDoor other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
