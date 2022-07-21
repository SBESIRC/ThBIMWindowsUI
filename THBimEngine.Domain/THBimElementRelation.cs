using System;

namespace THBimEngine.Domain
{
    public class THBimElementRelation : THBimElement
    {
        public int RelationElementId { get; set; }
        public string RelationElementUid { get; set; }
        public THBimElementRelation(int id, string name, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
