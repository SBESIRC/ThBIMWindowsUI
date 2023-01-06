using System;
using Xbim.Ifc;
using System.Linq;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Scene;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Geometries.Prepared;
using THBimEngine.Domain;

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

        public ThNTSIfcProductSpatialIndex(List<IfcProduct> products, bool precisionReduce = false, bool allowDuplicate = false)
        {
            // 默认使用固定精度
            PrecisionReduce = precisionReduce;
            // 默认忽略重复图元
            AllowDuplicate = allowDuplicate;

            Reset(products);
        }
        public void Dispose()
        {
            Geometries.Clear();
            Geometries = null;
            GeometryLookup = null;
            Engine = null;
        }
        private void Reset(IfcStore model)
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

        public void Reset(List<IfcProduct> products)
        {
            Geometries = new Dictionary<IfcProduct, Geometry>();
            Update(products, new List<IfcProduct>());
        }

        public void Update(List<IfcProduct> adds, List<IfcProduct> removals)
        {
            // 添加新的对象
            adds.ForEach(o =>
            {
                if (!Geometries.ContainsKey(o))
                {
                    Geometries[o] = o.ToNTSPolygon();
                }
            });

            // 移除删除对象
            removals.ForEach(o =>
            {
                if (Geometries.ContainsKey(o))
                {
                    Geometries.Remove(o);
                }
            });

            // 创建新的索引
            Engine = new STRtree<Geometry>();
            GeometryLookup = (Lookup<Geometry, IfcProduct>)Geometries.ToLookup(p => p.Value, p => p.Key);
            foreach (var item in GeometryLookup)
            {
                Engine.Insert(item.Key.EnvelopeInternal, item.Key);
            }
        }

        private List<IfcProduct> CrossingFilter(List<IfcProduct> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Intersects(preparedGeometry, o)).ToList();
        }

        private List<IfcProduct> FenceFilter(List<IfcProduct> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Intersects(preparedGeometry, o)).ToList();
        }

        private List<IfcProduct> WindowFilter(List<IfcProduct> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Contains(preparedGeometry, o)).ToList();
        }

        private bool Contains(IPreparedGeometry preparedGeometry, IfcProduct element)
        {
            return preparedGeometry.Contains(ToNTSGeometry(element));
        }

        public bool Intersects(IfcProduct element, bool precisely = false)
        {
            var geometry = ToNTSPolygonalGeometry(element);
            var queriedObjs = Query(geometry.EnvelopeInternal);

            if (precisely == false)
            {
                return queriedObjs.Count > 0;
            }

            var preparedGeometry = ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry);
            var hasIntersection = queriedObjs.Any(o => Intersects(preparedGeometry, o));
            return hasIntersection;
        }

        private bool Intersects(IPreparedGeometry preparedGeometry, IfcProduct element)
        {
            return preparedGeometry.Intersects(ToNTSGeometry(element));
        }

        private Geometry ToNTSGeometry(IfcProduct obj)
        {
            using (var ov = new ThIFCNTSFixedPrecision(PrecisionReduce))
            {
                return obj.ToNTSPolygon();
            }
        }

        private Polygon ToNTSPolygonalGeometry(IfcProduct obj)
        {
            using (var ov = new ThIFCNTSFixedPrecision(PrecisionReduce))
            {
                return obj.ToNTSPolygon();
            }
        }

        private Polygon ToNTSPolygonalGeometry(GeometryStretch geomStretch)
        {
            using (var ov = new ThIFCNTSFixedPrecision(PrecisionReduce))
            {
                return geomStretch.ToNTSPolygon();
            }
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        public List<IfcProduct> SelectCrossingPolygon(IfcProduct element)
        {
            var geometry = ToNTSPolygonalGeometry(element);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        public List<IfcProduct> SelectCrossingPolygon(GeometryStretch element)
        {
            var geometry = ToNTSPolygonalGeometry(element);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }


        /// <summary>
        /// Window selection
        /// </summary>
        public List<IfcProduct> SelectWindowPolygon(IfcProduct element)
        {
            var geometry = ToNTSPolygonalGeometry(element);
            return WindowFilter(Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        public List<IfcProduct> SelectFence(IfcProduct element)
        {
            var geometry = ToNTSGeometry(element);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public List<IfcProduct> SelectAll()
        {
            var objs = new List<IfcProduct>();
            foreach (var item in GeometryLookup)
            {
                if (AllowDuplicate)
                {
                    foreach (var e in item)
                    {
                        objs.Add(e);
                    }
                }
                else
                {
                    objs.Add(item.First());
                }
            }

            return objs;
        }

        public List<IfcProduct> Query(Envelope envelope)
        {
            var objs = new List<IfcProduct>();
            var results = Engine.Query(envelope).ToList();
            foreach (var item in GeometryLookup.Where(o => results.Contains(o.Key)))
            {
                if (AllowDuplicate)
                {
                    foreach (var e in item)
                    {
                        objs.Add(e);
                    }
                }
                else
                {
                    objs.Add(item.First());
                }
            }
            return objs;
        }
    }
}
