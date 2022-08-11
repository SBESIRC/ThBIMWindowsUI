using System;
using Xbim.Common.Geometry;

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
                ParentUid = relationElement.ParentUid;
            }
        }
        public THBimElementRelation(int id, string name, string relationElementUid,string parentUid, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            RelationElementUid = relationElementUid;
            ParentUid = parentUid;
        }
        public THBimElementRelation(int id, string name, string relationElementUid,int relationElementId, string parentUid, string describe = "", string uid = "") : base(id, name, describe, uid)
        {
            RelationElementUid = relationElementUid;
            RelationElementId = relationElementId;
            ParentUid = parentUid;
        }
        

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
