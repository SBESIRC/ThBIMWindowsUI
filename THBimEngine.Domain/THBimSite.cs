using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimSite : THBimElement
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
    }
}
