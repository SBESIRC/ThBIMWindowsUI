using System;

namespace THBimEngine.Domain
{
    public class THBimProject : THBimElement
    {
        public THBimSite ProjectSite { get; set; }
        public THBimProject(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
        }
        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
