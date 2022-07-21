using ProtoBuf;
using System.Collections.Generic;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHBuilding : ThTCHElement
    {
        [ProtoMember(11)]
        public string BuildingName { get; set; }
        [ProtoMember(12)]
        public List<ThTCHBuildingStorey> Storeys { get; set; }
        public ThTCHBuilding()
        {
            Storeys = new List<ThTCHBuildingStorey>();
        }

    }
}
