using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.IO.GFC2
{
    public static class THGFC2Extension
    {
        public static int AddGfc2String(this ThGFC2Document doc, string strValue)
        {
            int id = -1;
            if (doc.stringIndex.ContainsKey(strValue))
            {
                id = doc.stringIndex[strValue];
            }
            else
            {
                NGfc2String str = new NGfc2String();
                str.setValue(strValue);
                id = doc.AddEntity(str);
                doc.stringIndex.Add(strValue, id);
            }
            return id;
        }

        public static int AddGfc2Vector2d(this ThGFC2Document doc, double x, double y)
        {
            int id = -1;
            var tuple = new Tuple<double, double>(x, y);
            if (doc.vector2dIndex.ContainsKey(tuple))
            {
                id = doc.vector2dIndex[tuple];
            }
            else
            {
                NGfc2Vector2d v2d = new NGfc2Vector2d();
                v2d.setX(x);
                v2d.setY(y);
                id = doc.AddEntity(v2d);
                doc.vector2dIndex.Add(tuple, id);
            }
            return id;
        }

        public static int AddGfc2Vector3d(this ThGFC2Document doc, double x, double y, double z)
        {
            int id = -1;
            var tuple = new Tuple<double, double, double>(x, y, z);
            if (doc.vector3dIndex.ContainsKey(tuple))
            {
                id = doc.vector3dIndex[tuple];
            }
            else
            {
                NGfc2Vector3d v3d = new NGfc2Vector3d();
                v3d.setX(x);
                v3d.setY(y);
                v3d.setZ(z);
                id = doc.AddEntity(v3d);
                doc.vector3dIndex.Add(tuple, id);
            }
            return id;
        }

        /// <summary>
        /// 确定matrix3的是这么玩！！！！
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="matrixe3d"></param>
        /// <returns></returns>
        public static int AddGfc2Coordinates3d(this ThGFC2Document doc, XbimMatrix3D matrixe3d)
        {
            var id = -1;
            var xid = doc.AddGfc2Vector3d(matrixe3d.M11, matrixe3d.M12, matrixe3d.M13);
            var yid = doc.AddGfc2Vector3d(matrixe3d.M21, matrixe3d.M22, matrixe3d.M23);
            var zid = doc.AddGfc2Vector3d(matrixe3d.M31, matrixe3d.M32, matrixe3d.M33);
            var offsetId = doc.AddGfc2Vector3d(matrixe3d.OffsetX, matrixe3d.OffsetY, matrixe3d.OffsetZ);

            NGfc2Coordinates3d coordinates3D = new NGfc2Coordinates3d();
            coordinates3D.setX(xid);
            coordinates3D.setY(yid);
            coordinates3D.setZ(zid);
            coordinates3D.setOrigin(offsetId);
            id = doc.AddEntity(coordinates3D);

            return id;

        }

        public static int AddGfc2Line2d(this ThGFC2Document doc, XbimPoint3D sp, XbimPoint3D ep)
        {
            var id = -1;
            NGfc2Line2d ln = new NGfc2Line2d();
            int spId = doc.AddGfc2Vector2d(sp.X, sp.Y);
            int epId = doc.AddGfc2Vector2d(ep.X, ep.Y);
            ln.setStartPt(spId);
            ln.setEndPt(epId);

            id = doc.AddEntity(ln);
            return id;
        }

        public static int AddGfc2RectangleSection(this ThGFC2Document doc, double width, double height)
        {
            var id = -1;
            NGfc2RectangleSection rect = new NGfc2RectangleSection();
            rect.setWidth(width);
            rect.setHeight(height);

            id = doc.AddEntity(rect);
            return id;
        }

        public static int AddRelAggregate(this ThGFC2Document doc, int parentID, List<int> childIds)
        {
            var id = -1;
            if (childIds != null && childIds.Count > 0)
            {
                NGfc2RelAggregates rel = new NGfc2RelAggregates();
                rel.setRelatingObject(parentID);
                childIds.ForEach(x => rel.addRelatedObjects(x));

                id = doc.AddEntity(rel);
            }
            return id;
        }

        public static int AddRelDefinesByElement(this ThGFC2Document doc, int parentID, List<int> childIds)
        {
            var id = -1;
            if (childIds != null && childIds.Count > 0)
            {
                var rel = new NGfc2RelDefinesByElement();
                rel.setRelatingElement(parentID);
                childIds.ForEach(x => rel.addRelatedObjects(x));

                id = doc.AddEntity(rel);
            }
            return id;
        }
    }
}
