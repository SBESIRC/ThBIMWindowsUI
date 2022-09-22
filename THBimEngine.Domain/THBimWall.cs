using System;
using System.Collections.Generic;

namespace THBimEngine.Domain
{
    public class THBimWall : THBimEntity, IEquatable<THBimWall>
    {
        public IList<THBimDoor> Doors { get; private set; }
        public IList<THBimWindow> Windows { get; private set; }
        public THBimWall(int id, string name, string material, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, material, geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ Doors.Count ^ Windows.Count;
        }

        public bool Equals(THBimWall other)
        {
            if (!base.Equals(other)) return false;
            if (Doors.Count != other.Doors.Count) return false;
            if (Windows.Count != other.Windows.Count) return false;
            for (int i = 0; i < Doors.Count; i++)
            {
                if (!Doors[i].Equals(other.Doors[i]))
                {
                    return false;
                }
                if (!Windows[i].Equals(other.Windows[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
