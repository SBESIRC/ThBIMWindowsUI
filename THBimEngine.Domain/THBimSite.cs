using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimSite : THBimElement,IEquatable<THBimSite>
    {
        public List<THBimBuilding> SiteBuildings { get; }
        public THBimSite(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            SiteBuildings = new List<THBimBuilding>();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public bool Equals(THBimSite other)
        {
            if (!base.Equals(other)) return false;
            return true;
        }
    }
}
