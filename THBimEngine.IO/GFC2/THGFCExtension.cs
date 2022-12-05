using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common.Geometry;

namespace THBimEngine.IO.GFC2
{
    public static class THGFCExtension
    {
        public static int NewNGfc2String(this ThGFCDocument doc, string strValue)
        {
            int id = -1;
            if (doc.CommonStr.ContainsKey(strValue))
            {
                id = doc.CommonStr[strValue];
            }
            else
            {
                NGfc2String str = new NGfc2String();
                str.setValue(strValue);
                id = doc.writeEntity(str);
                doc.CommonStr.Add(strValue, id);
            }
            return id;
        }

        public static int NewNGfc2Vector2d(this ThGFCDocument doc, double x, double y)
        {
            int id = -1;
            var tuple = new Tuple<double, double>(x, y);
            if (doc.CommonVector2d.ContainsKey(tuple))
            {
                id = doc.CommonVector2d[tuple];
            }
            else
            {
                NGfc2Vector2d v2d = new NGfc2Vector2d();
                v2d.setX(x);
                v2d.setY(y);
                id = doc.writeEntity(v2d);
                doc.CommonVector2d.Add(tuple, id);
            }
            return id;
        }

        public static int NewNGfc2Vector3d(this ThGFCDocument doc, double x, double y, double z)
        {
            int id = -1;
            var tuple = new Tuple<double, double, double>(x, y, z);
            if (doc.CommonVector3d.ContainsKey(tuple))
            {
                id = doc.CommonVector3d[tuple];
            }
            else
            {
                NGfc2Vector3d v3d = new NGfc2Vector3d();
                v3d.setX(x);
                v3d.setY(y);
                v3d.setZ(z);
                id = doc.writeEntity(v3d);
                doc.CommonVector3d.Add(tuple, id);
            }
            return id;
        }

        /// <summary>
        /// 确定matrix3的是这么玩！！！！
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="matrixe3d"></param>
        /// <returns></returns>
        public static int NewNGfc2Coordinates3d(this ThGFCDocument doc, XbimMatrix3D matrixe3d)
        {
            var xid = doc.NewNGfc2Vector3d(matrixe3d.M11, matrixe3d.M12, matrixe3d.M13);
            var yid = doc.NewNGfc2Vector3d(matrixe3d.M21, matrixe3d.M22, matrixe3d.M23);
            var zid = doc.NewNGfc2Vector3d(matrixe3d.M31, matrixe3d.M32, matrixe3d.M33);
            var offsetId = doc.NewNGfc2Vector3d(matrixe3d.OffsetX, matrixe3d.OffsetY, matrixe3d.OffsetZ);

            NGfc2Coordinates3d coordinates3D = new NGfc2Coordinates3d();
            coordinates3D.setX(xid);
            coordinates3D.setY(yid);
            coordinates3D.setZ(zid);
            coordinates3D.setOrigin(offsetId);
            return doc.writeEntity(coordinates3D);

        }

        public static int NewNGfc2Line2d(this ThGFCDocument doc, XbimPoint3D sp, XbimPoint3D ep)
        {
            NGfc2Line2d ln = new NGfc2Line2d();
            int spId = doc.NewNGfc2Vector2d(sp.X, sp.Y);
            int epId = doc.NewNGfc2Vector2d(ep.X, ep.Y);
            ln.setStartPt(spId);
            ln.setEndPt(epId);
            return doc.writeEntity(ln);
        }

        public static int NewNGfc2RectangleSection(this ThGFCDocument doc, double width, double height)
        {
            NGfc2RectangleSection rect = new NGfc2RectangleSection();
            rect.setWidth(width);
            rect.setHeight(height);
            return doc.writeEntity(rect);
        }


    }
}
