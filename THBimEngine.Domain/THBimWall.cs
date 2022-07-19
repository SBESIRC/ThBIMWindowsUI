using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public class THBimWall : THBimEntity,ICloneable
    {
        public THBimWall()
        {

        }

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
