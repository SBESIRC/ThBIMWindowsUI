using System;

namespace THBimEngine.Domain
{
    public class THBimElementRelation : THBimElement
    {
        public int RelationElementId { get; set; }
        public string RelationElementUid { get; set; }
        public THBimElementRelation(int id, string name, THBimElement relationElement =null, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            if (null != relationElement) 
            {
                RelationElementUid = relationElement.Uid;
                RelationElementId = relationElement.Id;
            }
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
