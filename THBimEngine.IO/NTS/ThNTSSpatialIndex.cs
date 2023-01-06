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
    public class ThNTSSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        private Dictionary<Geometry, Geometry> Geometries { get; set; }
        private Lookup<Geometry, Geometry> GeometryLookup { get; set; }
        public bool AllowDuplicate { get; set; }
        public bool PrecisionReduce { get; set; }
        private ThNTSSpatialIndex() { }

        public ThNTSSpatialIndex(List<Geometry> products, bool precisionReduce = false, bool allowDuplicate = false)
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

        public void Reset(List<Geometry> products)
        {
            Geometries = new Dictionary<Geometry, Geometry>();
            Update(products, new List<Geometry>());
        }

        public void Update(List<Geometry> adds, List<Geometry> removals)
        {
            // 添加新的对象
            adds.ForEach(o =>
            {
                if (!Geometries.ContainsKey(o))
                {
                    Geometries[o] = o;
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
            GeometryLookup = (Lookup<Geometry, Geometry>)Geometries.ToLookup(p => p.Value, p => p.Key);
            foreach (var item in GeometryLookup)
            {
                Engine.Insert(item.Key.EnvelopeInternal, item.Key);
            }
        }

        private List<Geometry> CrossingFilter(List<Geometry> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Intersects(preparedGeometry, o)).ToList();
        }

        private List<Geometry> FenceFilter(List<Geometry> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Intersects(preparedGeometry, o)).ToList();
        }

        private List<Geometry> WindowFilter(List<Geometry> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Contains(preparedGeometry, o)).ToList();
        }

        private bool Contains(IPreparedGeometry preparedGeometry, Geometry element)
        {
            return preparedGeometry.Contains(element);
        }

        public bool Intersects(Geometry element, bool precisely = false)
        {
            var geometry = element;
            var queriedObjs = Query(geometry.EnvelopeInternal);

            if (precisely == false)
            {
                return queriedObjs.Count > 0;
            }

            var preparedGeometry = ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry);
            var hasIntersection = queriedObjs.Any(o => Intersects(preparedGeometry, o));
            return hasIntersection;
        }

        private bool Intersects(IPreparedGeometry preparedGeometry, Geometry element)
        {
            return preparedGeometry.Intersects(element);
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        public List<Geometry> SelectCrossingPolygon(Polygon element)
        {
            var geometry = element;
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Window selection
        /// </summary>
        public List<Geometry> SelectWindowPolygon(Polygon element)
        {
            var geometry = element;
            return WindowFilter(Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        public List<Geometry> SelectFence(Geometry element)
        {
            var geometry = element;
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public List<Geometry> SelectAll()
        {
            var objs = new List<Geometry>();
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

        public List<Geometry> Query(Envelope envelope)
        {
            var objs = new List<Geometry>();
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
