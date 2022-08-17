using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public enum BuildingCatagory
    {
        [Description("结构")]
        Structure =10,
        [Description("建筑")]
        Architecture =20,
        [Description("暖通")]
        HVAC =30,
        [Description("电气")]
        Electrical =40,
        [Description("水")]
        Water =50,

        Wall =100010,
        StructureWall = 100011,
        ArchitectureWall =100012,
        Beam =100013,
        Column =100014,
        Roof =100015,
        Slab = 100016,
        Door =100020,
        Window =100021,
        Railing =100022,
        Opening =100099,
    }
}
