using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.OverlayNG;

using ThBIMServer.NTS;
using THBimEngine.Domain;
using NetTopologySuite.Algorithm;

namespace XbimXplorer.Deduct
{
    internal static class DeductService
    {
        /// <summary>
        /// union S墙和a墙intersect，去除掉太小的部分，剩下的每一部分obb
        /// 如果obb和原a墙面积差不多，则保留原墙
        /// 否则，保留切出来的obb
        /// 如果返回值count=0  => onlyDelete = true 只删除墙， onlyDelete = false 则保留原墙
        /// 返回值count>0 -> 删除原墙，用新墙代替
        /// 
        /// 还有问题切出来各种问题
        /// </summary>
        /// <param name="archiWallOri"></param>
        /// <param name="geomStructList"></param>
        /// <returns></returns>
        public static List<Polygon> CutBimWallGeom(Polygon archiWallOri, List<Polygon> geomStructList, out bool onlyDelete)
        {
            var cutPart = new List<Polygon>();
            var tol_tooSmallCut = 50;
            var tol_OBBRatio = 0.95;
            var tol_buffer = 100;

            onlyDelete = false;
            var geomStructBufferList = geomStructList.Select(x => BufferWall(x, tol_buffer)).ToList();
            var geomStructUnion = OverlayNGRobust.Union(geomStructBufferList);

            if (geomStructUnion.Contains(archiWallOri))
            {
                onlyDelete = true;
            }
            else
            {
                var cutArchiWallPolyTemp = new List<Polygon>();
                var cutArchiWall = archiWallOri.Difference(geomStructUnion);
                if (cutArchiWall is GeometryCollection collect)
                {
                    cutArchiWallPolyTemp.AddRange(collect.Geometries.OfType<Polygon>().ToList());
                }
                else if (cutArchiWall is Polygon cutArchiWallpl)
                {
                    cutArchiWallPolyTemp.Add(cutArchiWallpl);
                }


                var cutArchiWallPoly = new List<LinearRing>();
                cutArchiWallPoly.AddRange(cutArchiWallPolyTemp.Select(x => x.Shell));

                //var cutArchiWallNotSmall = RemoveTooSmallCutWall(cutArchiWallPoly, tol_tooSmallCut);


                var cutPolyObb = cutArchiWallPoly.Select(x => x.ToObb()).ToList();
                var isOriWall = IsCutWallSimilarWithOri(cutPolyObb, archiWallOri, tol_OBBRatio);
                if (isOriWall == false)
                {
                    cutPart.AddRange(cutPolyObb);
                }
            }

            return cutPart;

        }

        /// <summary>
        /// 有一个跟原墙一样就不作处理
        /// </summary>
        /// <param name="cutPolyObb"></param>
        /// <param name="archiWallOri"></param>
        /// <param name="tol_OBBRatio"></param>
        /// <returns></returns>
        private static bool IsCutWallSimilarWithOri(List<Polygon> cutPolyObb, Polygon archiWallOri, double tol_OBBRatio)
        {
            var isOriWall = cutPolyObb.Where(x => x.Area / archiWallOri.Area >= tol_OBBRatio ||
                                                  x.Area / archiWallOri.Area <= (1 + (1 - tol_OBBRatio))).Any(); //有可能obb比原大
            return isOriWall;
        }

        /// <summary>
        /// 用最短边长度 小于 tol 50,
        /// 这里还不能用单纯的最小边，再考虑
        /// </summary>
        /// <param name="cutArchiWallPoly"></param>
        /// <returns></returns>
        private static List<LinearRing> RemoveTooSmallCutWall(List<LinearRing> cutArchiWallPoly, double tol)
        {
            var notsmall = new List<LinearRing>();

            notsmall.AddRange(cutArchiWallPoly);


            return notsmall;
        }

        private static Polygon ToObb(this Geometry geom)
        {
            var rectangle = MinimumDiameter.GetMinimumRectangle(geom);
            if (rectangle is Polygon polygon)
            {
                return polygon;
            }
            else
            {
                throw new NotSupportedException();
            }
        }


        /// <summary>
        /// 找短边 flat buffer，写的很硬，容易出问题
        /// </summary>
        /// <param name="wall"></param>
        /// <returns></returns>
        private static Polygon BufferWall(Polygon wall, double tol)
        {
            Polygon bufferWall = null;

            var pt0 = wall.Coordinates[0];
            var pt1 = wall.Coordinates[1];
            var pt2 = wall.Coordinates[2];
            var pt3 = wall.Coordinates[3];

            //var bufferInfo = new BufferOp(wall, new BufferParameters()
            //{
            //    JoinStyle = JoinStyle.Mitre,
            //    EndCapStyle = EndCapStyle.Flat,
            //});

            //var buffergeom = bufferInfo.GetResultGeometry(500);

            //if (buffergeom is LinearRing linearRing)
            //{
            //    bufferWall = linearRing.CreatePolygon();
            //}
            //else if (buffergeom is Polygon polygon)
            //{
            //    bufferWall = polygon;
            //}

            var bufferCoors = new List<Coordinate>();
            Coordinate p0 = null;
            Coordinate p1 = null;
            Coordinate p2 = null;
            Coordinate p3 = null;

            if (pt0.Distance(pt1) <= pt1.Distance(pt2))
            {
                var shortDir = new Vector2D(pt0, pt1).Normalize();
                p0 = pt0.Offset((-shortDir * tol).ToCoordinate());
                p1 = pt1.Offset((shortDir * tol).ToCoordinate());
                p2 = pt2.Offset((shortDir * tol).ToCoordinate());
                p3 = pt3.Offset((-shortDir * tol).ToCoordinate());
            }
            else
            {
                var shortDir = new Vector2D(pt1, pt2).Normalize();
                p0 = pt0.Offset((-shortDir * tol).ToCoordinate());
                p1 = pt1.Offset((-shortDir * tol).ToCoordinate());
                p2 = pt2.Offset((shortDir * tol).ToCoordinate());
                p3 = pt3.Offset((shortDir * tol).ToCoordinate());
            }

            bufferCoors.Add(p0);
            bufferCoors.Add(p1);
            bufferCoors.Add(p2);
            bufferCoors.Add(p3);
            bufferCoors.Add(p0);

            bufferWall = bufferCoors.CreateLineString().CreatePolygon();
            return bufferWall;
        }

        public static List<THBimWall> ToThBimWall(THBimWall oriWall, List<Polygon> newWallOutline)
        {
            var newBimWall = new List<THBimWall>();

            for (int i = 0; i < newWallOutline.Count; i++)
            {
                var geom = oriWall.GeometryParam.Clone() as GeometryParam;
                if (oriWall.GeometryParam is GeometryStretch)
                {
                    foreach (var pt in newWallOutline[i].Shell.Coordinates)
                    {
                        ((GeometryStretch)geom).Outline = new ThTCHMPolygon();
                }
                }


                var bimWall = new THBimWall(Convert.ToInt32(oriWall.Id.ToString() + i.ToString()), oriWall.Name, oriWall.Material, geom, "", wall.BuildElement.Root.GlobalId);

            }










            return newBimWall;
        }
    }
}
