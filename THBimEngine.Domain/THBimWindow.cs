using System;

namespace THBimEngine.Domain
{
    public class THBimWindow : THBimEntity, IEquatable<THBimOpening>
    {
        /// <summary>
        /// 窗类型
        /// </summary>
        public uint WindowType { get; set; }

        public THBimWindow(int id, string name, string material, GeometryParam geometryParam, uint windowType, string describe = "", string uid = "") : base(id, name,"", geometryParam, describe, uid)
        {
            WindowType = windowType;
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool Equals(THBimOpening other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
