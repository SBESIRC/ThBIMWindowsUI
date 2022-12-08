using System;
using System.Linq;
using System.Collections.Generic;

using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometryResource;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Geometries.Prepared;

using THBimEngine.Domain;
using Google.Protobuf;

namespace ThBIMServer.NTS
{
    public class ThGPNTSSpatialIndex : IDisposable
    {
        private STRtree<Geometry> Engine { get; set; }
        private Dictionary<IBufferMessage, Geometry> Geometries { get; set; }
        private Lookup<Geometry, IBufferMessage> GeometryLookup { get; set; }
        public bool AllowDuplicate { get; set; }
        public bool PrecisionReduce { get; set; }

        private ThGPNTSSpatialIndex()
        {

        }

        public ThGPNTSSpatialIndex(List<IBufferMessage> profiles, bool precisionReduce = false, bool allowDuplicate = false)
        {
            // 默认使用固定精度
            PrecisionReduce = precisionReduce;
            // 默认忽略重复图元
            AllowDuplicate = allowDuplicate;

            Reset(profiles);
        }

        public void Dispose()
        {
            Geometries.Clear();
            Geometries = null;
            GeometryLookup = null;
            Engine = null;
        }

        private List<IBufferMessage> CrossingFilter(List<IBufferMessage> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Intersects(preparedGeometry, o)).ToList();
        }

        private List<IBufferMessage> FenceFilter(List<IBufferMessage> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Intersects(preparedGeometry, o)).ToList();
        }

        private List<IBufferMessage> WindowFilter(List<IBufferMessage> objs, IPreparedGeometry preparedGeometry)
        {
            return objs.Where(o => Contains(preparedGeometry, o)).ToList();
        }

        private bool Contains(IPreparedGeometry preparedGeometry, IBufferMessage entity)
        {
            return preparedGeometry.Contains(ToNTSGeometry(entity));
        }

        public bool Intersects(IBufferMessage entity, bool precisely = false)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            var queriedObjs = Query(geometry.EnvelopeInternal);

            if (precisely == false)
            {
                return queriedObjs.Count > 0;
            }

            var preparedGeometry = ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry);
            var hasIntersection = queriedObjs.Any(o => Intersects(preparedGeometry, o));
            return hasIntersection;
        }

        private bool Intersects(IPreparedGeometry preparedGeometry, IBufferMessage entity)
        {
            return preparedGeometry.Intersects(ToNTSGeometry(entity));
        }

        private Geometry ToNTSGeometry(IBufferMessage obj)
        {
            using (var ov = new ThIFCNTSFixedPrecision(PrecisionReduce))
            {
                return obj.ToNTSPolygon();
            }
        }

        private Polygon ToNTSPolygonalGeometry(IBufferMessage obj)
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
        /// 更新索引
        /// </summary>
        /// <param name="adds"></param>
        /// <param name="removals"></param>
        public void Update(List<IBufferMessage> adds, List<IBufferMessage> removals)
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
            GeometryLookup = (Lookup<Geometry, IBufferMessage>)Geometries.ToLookup(p => p.Value, p => p.Key);
            foreach (var item in GeometryLookup)
            {
                Engine.Insert(item.Key.EnvelopeInternal, item.Key);
            }
        }

        /// <summary>
        /// 重置索引
        /// </summary>
        public void Reset(List<IBufferMessage> profiles)
        {
            Geometries = new Dictionary<IBufferMessage, Geometry>();
            Update(profiles, new List<IBufferMessage>());
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<IBufferMessage> SelectCrossingPolygon(ThTCHMPolygon entity)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Crossing selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<IBufferMessage> SelectCrossingPolygon(GeometryStretch entity)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            return CrossingFilter(
                Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }


        /// <summary>
        /// Window selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<IBufferMessage> SelectWindowPolygon(ThTCHMPolygon entity)
        {
            var geometry = ToNTSPolygonalGeometry(entity);
            return WindowFilter(Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        /// <summary>
        /// Fence Selection
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public List<IBufferMessage> SelectFence(ThTCHMPolygon entity)
        {
            var geometry = ToNTSGeometry(entity);
            return FenceFilter(Query(geometry.EnvelopeInternal),
                ThIFCNTSService.Instance.PreparedGeometryFactory.Create(geometry));
        }

        public List<IBufferMessage> SelectAll()
        {
            var objs = new List<IBufferMessage>();
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

        public List<IBufferMessage> Query(Envelope envelope)
        {
            var objs = new List<IBufferMessage>();
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
