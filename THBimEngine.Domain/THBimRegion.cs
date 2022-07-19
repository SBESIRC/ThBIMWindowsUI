using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    #region 基础数据
    public class THBimRegion
    {
        public List<THBimLine> LineSengments { get; }
        public List<THBimRegion> Holes { get; }
    }
    public class THBimLine
    {
        public XbimPoint3D StartPoint { get; set; }
        public XbimPoint3D MidPoint { get; set; }
        public XbimPoint3D EndPoint { get; set; }
        public bool IsArc()
        {
            return false;
        }
    }
    #endregion
}
