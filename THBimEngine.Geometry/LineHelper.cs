using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.Geometry
{
    class LineHelper
    {
        /// <summary>
        /// 直线与直线相交(XOY平面)
        /// </summary>
        /// <param name="s0"></param>
        /// <param name="dir1"></param>
        /// <param name="isRay"></param>
        /// <param name="s1"></param>
        /// <param name="dir2"></param>
        /// <param name="intersectionPoint"></param>
        /// <returns>
        /// 0: 不相交
        /// 1: 只有一个交点
        /// 2: 共线
        /// </returns>
        public static int FindIntersection(XbimPoint3D s0, XbimVector3D dir1, XbimPoint3D s1, XbimVector3D dir2, out XbimPoint3D intersectionPoint)
        {
            intersectionPoint = XbimPoint3D.Zero;
            double Linear = 0.000000001;
            var P0 = s0;
            var D0 = dir1;
            var P1 = s1;
            var D1 = dir2;
            var E = P1 - P0;
            var kross = D0.X * D1.Y - D0.Y * D1.X;
            var sqrKross = kross * kross;
            var sqrLen0 = D0.X * D0.X + D0.Y * D0.Y;
            var sqrLen1 = D1.X * D1.X + D1.Y * D1.Y;
            var sqlEpsilon = Linear * Linear;
            //有一个交点
            if (sqrKross > sqlEpsilon * sqrLen0 * sqrLen1)
            {
                var s = (E.X * D1.Y - E.Y * D1.X) / kross;
                intersectionPoint = P0 + s * D0;
                return 1;
            }
            //如果线是平行的
            var sqrLenE = E.X * E.X + E.Y * E.Y;
            kross = E.X * D0.Y - E.Y * D0.X;
            sqrKross = kross * kross;

            var value = sqlEpsilon * sqrLen0 * sqrLenE;
            if (Math.Abs(sqrKross - value) > Linear && sqrKross > value)
                return 0;
            return 2;
        }
    }
}
