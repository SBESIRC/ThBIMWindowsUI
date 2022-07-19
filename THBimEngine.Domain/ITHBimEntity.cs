using System;
using System.Collections.Generic;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    #region 基础实体
    public class THBimEntity : ITHBimElement
    {
        public int Index { get; set; }

        #region 二维几何信息
        public double XAxisLength { get; set; }
        public double YAxisLength { get; set; }
        public double ZAxisLength { get; set; }
        public XbimPoint3D Origin { get; set; }
        public XbimVector3D ZAxis { get; set; }
        public XbimVector3D XAxis { get; set; }
        public THBimRegion OutLine { get; set; }
        #endregion

        public Dictionary<string, object> Props { get; set; }

    }

    //public THBimEntity: ITHBimEntity
    //{
    //    ifcEntity entity;
    //}

    
    
    //... 门、窗、栏杆、楼板、洞口
    #endregion

    #region 分类、楼层
    #endregion
}
