using System;

namespace THBimEngine.Domain
{
    public class THBimProject : THBimElement,IEquatable<THBimProject>
    {
        public THBimSite ProjectSite { get; set; }
        public THBimProject(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(THBimProject other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
