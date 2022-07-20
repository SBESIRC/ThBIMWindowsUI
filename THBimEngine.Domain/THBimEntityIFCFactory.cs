using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Tessellator;

namespace THBimEngine.Domain
{
    class THBimEntityIFCFactory
    {
        public THBimEntityIFCFactory(IfcExtrudedAreaSolid areaSolid) 
        {
            XbimTessellator tessellator = new XbimTessellator(null, XbimGeometryType.PolyhedronBinary);
            if (tessellator.CanMesh(areaSolid)) 
            {
                var res = tessellator.Mesh(areaSolid);
            }

        }
    }
}
