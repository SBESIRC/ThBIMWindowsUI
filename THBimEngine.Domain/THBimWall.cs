using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public class THBimWall : THBimEntity
    {
        public IList<THBimDoor> Doors { get; private set; }
        public IList<THBimWindow> Windows { get; private set; }
        public THBimWall(int id, string name, GeometryParam geometryParam, string describe = "", string uid = "") : base(id, name, geometryParam, describe, uid)
        {
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
