﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Domain
{
    public class THBimSlab : THBimEntity
    {
        public THBimSlab(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name,geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
