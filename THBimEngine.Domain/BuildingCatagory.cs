using System.ComponentModel;

namespace THBimEngine.Domain
{
    public enum BuildingCatagory
    {
        [Description("墙")]
        Wall =100010,
        [Description("结构墙")]
        StructureWall = 100011,
        [Description("建筑墙")]
        ArchitectureWall =100012,
        [Description("梁")]
        Beam =100013,
        [Description("柱")]
        Column =100014,
        [Description("屋面")]
        Roof =100015,
        [Description("楼板")]
        Slab = 100016,
        [Description("轴网")]
        Grid = 100017,
        [Description("门")]
        Door =100020,
        [Description("窗")]
        Window =100021,
        [Description("栏杆")]
        Railing =100022,
        [Description("洞口")]
        Opening =100099,
        [Description("无类型")]//需要后续计算的
        UnTypeElement = 9999998,
        [Description("未知")]
        Unknown = 9999999,
    }
}
