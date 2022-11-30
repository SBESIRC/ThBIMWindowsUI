using System.Collections.Generic;
using System.Linq;

namespace THBimEngine.Domain
{
    public class ThBimStoreyManager
    {
        /// <summary>
        /// 楼层名
        /// </summary>
        public string Name
        {
            get
            {
                /*
                var arch = Storeys.Where(o => o.Key.Equals(EMajor.Architecture)).ToList();
                if (arch.Count > 0)
                {
                    return arch[0].Value.First().Name;
                }*/

                var stru = Storeys.Where(o => o.Key.Equals(EMajor.Structure)).ToList();
                if (stru.Count > 0)
                {
                    return stru[0].Value.First().Name;
                }

                return "";
            }
        }

        /// <summary>
        /// 建筑标高信息
        /// </summary>
        private ThBimElevationInfo ArchElevation
        {
            get
            {
                /*
                var arch = Storeys.Where(o => o.Key.Equals(EMajor.Architecture)).ToList();
                if (arch.Count > 0)
                {
                    return new ThBimElevationInfo(true, arch[0].Value.First().Elevation);
                }*/

                return new ThBimElevationInfo(false, 0.0);
            }
        }

        /// <summary>
        /// 结构标高信息
        /// </summary>
        private ThBimElevationInfo StruElevation
        {
            get
            {
                var stru = Storeys.Where(o => o.Key.Equals(EMajor.Structure)).ToList();
                if (stru.Count > 0)
                {
                    return new ThBimElevationInfo(true, stru[0].Value.First().Elevation);
                }

                return new ThBimElevationInfo(false, 0.0);
            }
        }

        /// <summary>
        /// 标高
        /// </summary>
        public double Elevation
        {
            get
            {
                return ArchElevation.HasValue ? ArchElevation.Elevation : (StruElevation.HasValue ? StruElevation.Elevation : 0.0);
            }
        }

        /// <summary>
        /// 层高
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// 楼层引用
        /// </summary>
        public Dictionary<EMajor, List<THBimStorey>> Storeys { get; set; }

        public ThBimStoreyManager(double height)
        {
            Height = height;
            Storeys = new Dictionary<EMajor, List<THBimStorey>>();
        }
    }

    public class ThBimElevationInfo
    {
        /// <summary>
        /// 是否包含该类型楼层
        /// </summary>
        public bool HasValue { get; set; }

        /// <summary>
        /// 标高
        /// </summary>
        public double Elevation { get; set; }

        public ThBimElevationInfo(bool isContains, double elevation)
        {
            HasValue = isContains;
            Elevation = elevation;
        }
    }
}
