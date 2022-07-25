using ProtoBuf;
using System.Collections.Generic;
using THBimEngine.Domain.Model.SurrogateModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    public class ThTCHBuildingStorey : ThTCHElement
    {
        /// <summary>
        /// 编号
        /// </summary>
        [ProtoMember(21)]
        public string Number { get; set; }
        /// <summary>
        /// 标高
        /// </summary>
        [ProtoMember(22)]
        public double Elevation { get; set; }
        public ThTCHBuildingStorey()
        {
            Walls = new List<ThTCHWall>();
            Doors = new List<ThTCHDoor>();
            Slabs = new List<ThTCHSlab>();
            Windows = new List<ThTCHWindow>();
            Railings = new List<ThTCHRailing>();
        }
        [ProtoMember(31)]
        public List<ThTCHWall> Walls { get; set; }
        [ProtoMember(32)]
        public List<ThTCHWindow> Windows { get; set; }
        [ProtoMember(33)]
        public List<ThTCHDoor> Doors { get; set; }
        [ProtoMember(34)]
        public List<ThTCHSlab> Slabs { get; set; }
        [ProtoMember(35)]
        public List<ThTCHRailing> Railings { get; set; }
        [ProtoMember(98)]
        public string MemoryStoreyId { get; set; }
        [ProtoMember(99)]
        public Matrix3DSurrogate MemoryMatrix3d { get; set; }
    }
}
