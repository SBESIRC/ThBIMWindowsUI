using ProtoBuf;
using System.Collections.Generic;

namespace THBimEngine.Presention.Model
{
    [ProtoContract]
    public class ThTCHBuilding //: ThIfcBuilding
    {
        [ProtoMember(1)]
        public string BuildingName { get; set; }
        [ProtoMember(2)]
        public List<ThTCHBuildingStorey> Storeys { get; set; }
        public ThTCHBuilding()
        {
            Storeys = new List<ThTCHBuildingStorey>();
        }

    }
}
