using System;
using Xbim.Ifc;
using System.Linq;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Scene;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;

namespace ThBIMServer.NTS
{
    public class ThNTSIfcProductSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        private Dictionary<IfcProduct, Geometry> Geometries { get; set; }
        private Lookup<Geometry, IfcProduct> GeometryLookup { get; set; }
        public bool AllowDuplicate { get; set; }
        public bool PrecisionReduce { get; set; }
        private ThNTSIfcProductSpatialIndex() { }
        public ThNTSIfcProductSpatialIndex(IfcStore model, bool precisionReduce = false, bool allowDuplicate = false)
        {
            // 默认使用固定精度
            PrecisionReduce = precisionReduce;
            // 默认忽略重复图元
            AllowDuplicate = allowDuplicate;

            Reset(model);
        }
        public void Dispose()
        {
            Geometries.Clear();
            Geometries = null;
            GeometryLookup = null;
            Engine = null;
        }
        public void Reset(IfcStore model)
        {
            // 从IfcStore创建三维场景（利用Xbim3DModelContext)
            // 从IfcStore中获取构件，从三维场景中获取对应构件的变换矩阵
            // 对于拉伸体构件（IfcExtrudedAreaSolid）：
            //  1. 获取其SweptArea(IfcProfileDef)
            //  2. 将IfcProfileDef转换到全局坐标系
            //  4. 用IfcProfileDef构建2D场景
            // 前提条件：
            //  1. ExtrudedDirection(0,0,1)
            //  2. No Position coordinate system
            var modelContext = new Xbim3DModelContext(model);
        }
    }
}
