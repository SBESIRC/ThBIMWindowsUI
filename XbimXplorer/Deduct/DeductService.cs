using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NetTopologySuite.Algorithm;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using NetTopologySuite.Operation.Buffer;
using NetTopologySuite.Operation.OverlayNG;

using ThBIMServer.NTS;
using THBimEngine.Domain;
using THBimEngine.Geometry.ProjectFactory;
using THBimEngine.IO.NTS;

using XbimXplorer.Deduct.Model;


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
        public static List<Polygon> CutBimWallGeom(DeductGFCModel archiWall, List<DeductGFCModel> structWallList, out bool onlyDelete, int printC = -1, string printPath = "")
        {
            var cutPart = new List<Polygon>();
            var tol_tooSmallCut = 201;
            var tol_OBBRatio = 0.95;
            var tol_buffer = 100;
            var tol_Simplify = 1;
            var tol_angleSA = 3 * Math.PI / 180;

            onlyDelete = false;

            var geomArchi = archiWall.Outline;
            var geomStructList = structWallList.Select(x => x.Outline).ToList();

            var geomStructBufferList = structWallList.Select(x => BufferWall(x, archiWall, tol_buffer, tol_angleSA)).ToList();
            var geomStructUnion = OverlayNGRobust.Union(geomStructBufferList);

            if (geomStructUnion.Contains(geomArchi))
            {
                onlyDelete = true;
            }
            else
            {
                var cutArchiWallPolyTemp = new List<Polygon>();
                var cutArchiWall = geomArchi.Difference(geomStructUnion);
                if (cutArchiWall is GeometryCollection collect)
                {
                    cutArchiWallPolyTemp.AddRange(collect.Geometries.OfType<Polygon>().ToList());
                }
                else if (cutArchiWall is Polygon cutArchiWallpl)
                {
                    cutArchiWallPolyTemp.Add(cutArchiWallpl);
                }

                var cutArchiWallPoly = new List<Polygon>();
                cutArchiWallPoly.AddRange(cutArchiWallPolyTemp.SelectMany(x => x.SimplifyPl(tol_Simplify)));

                //var cutArchiWallNotSmall = RemoveTooSmallCutWallArea(cutArchiWallPoly, tol_tooSmallCut);
                var cutArchiWallNotSmall = new List<Polygon>();
                cutArchiWallNotSmall.AddRange(cutArchiWallPoly);
                var cutPolyObb = cutArchiWallNotSmall.Where(x => x.Coordinates.Count() > 0).Select(x => x.ToObb()).ToList();

                var cutPolyObbNotSmall = RemoveTooSmallCutWallShortSide(cutPolyObb, tol_tooSmallCut);

                if (cutPolyObbNotSmall.Count == 0)
                {
                    onlyDelete = true;
                }
                else
                {
                    var isOriWall = IsCutWallSimilarWithOri(cutPolyObbNotSmall, geomArchi, tol_OBBRatio);

                    if (isOriWall == false)
                    {
                        cutPart.AddRange(cutPolyObbNotSmall);
                    }
                }

                if (printC != -1)
                {
                    var script = "";
                    DeductCommonService.DebugScript(new List<Polygon> { geomArchi }, "l0wallOri", 0, printC, ref script);
                    DeductCommonService.DebugScript(geomStructList, "l0wallStruc", 1, printC, ref script);
                    DeductCommonService.DebugScript(geomStructBufferList, "l1wallStrucBuff", 2, printC, ref script);
                    DeductCommonService.DebugScript(cutArchiWallPolyTemp, "l2cutArchiWallPolyTemp", 3, printC, ref script);
                    DeductCommonService.DebugScript(cutArchiWallPoly, "l3cutArchiWallPoly", 4, printC, ref script);
                    DeductCommonService.DebugScript(cutArchiWallNotSmall, "l4cutArchiWallNotSmall", 5, printC, ref script);
                    DeductCommonService.DebugScript(cutPolyObb, "l5cutPolyObb", 6, printC, ref script);
                    DeductCommonService.DebugScript(cutPolyObbNotSmall, "l6cutPolyObbNotSmall", 11, printC, ref script);

                    using (var fs = new System.IO.StreamWriter(printPath, true))
                    {
                        fs.Write(script);
                        fs.Flush();
                        fs.Close();
                    }
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
            var isOriWall = cutPolyObb.Where(x => x.Area / archiWallOri.Area >= tol_OBBRatio &&
                                                  x.Area / archiWallOri.Area <= (1 + (1 - tol_OBBRatio))).Any(); //有可能obb比原大
            return isOriWall;
        }

        private static List<Polygon> RemoveTooSmallCutWallArea(List<Polygon> cutArchiWallPoly, double tol)
        {
            var notsmall = new List<Polygon>();

            foreach (var wallPoly in cutArchiWallPoly)
            {

                if (wallPoly.Area > 200 * tol)
                {
                    notsmall.Add(wallPoly);
                }
            }

            return notsmall;
        }

        /// <summary>
        /// 最长边小于200的
        /// </summary>
        /// <param name="cutArchiWallPoly"></param>
        /// <param name="tol"></param>
        /// <returns></returns>
        public static List<Polygon> RemoveTooSmallCutWallShortSide(List<Polygon> cutArchiWallPoly, double tol)
        {
            var notsmall = new List<Polygon>();

            foreach (var wallPoly in cutArchiWallPoly)
            {
                var isShort = IsMaxSideShort(wallPoly, tol);
                if (!isShort)
                {
                    notsmall.Add(wallPoly);
                }
            }

            return notsmall;
        }

        private static bool IsMaxSideShort(Polygon pl, double tol)
        {
            var returnB = false;

            var maxSideD = 0.0;
            for (int i = 0; i < pl.Coordinates.Count() - 1; i++)
            {
                var dist = pl.Coordinates[i].Distance(pl.Coordinates[i + 1]);
                if (dist >= maxSideD)
                {
                    maxSideD = dist;
                }
            }
            if (maxSideD < tol)
            {
                returnB = true;
            }


            return returnB;
        }

        /// <summary>
        /// 两墙垂直，结构不做buffer
        /// 平行buffer直接用结构的中心线外扩半个厚度+buffer
        /// </summary>
        /// <param name="strucWall"></param>
        /// <param name="archiWall"></param>
        /// <param name="tol"></param>
        /// <param name="angleTol"></param>
        /// <returns></returns>
        public static Polygon BufferWall(DeductGFCModel strucWall, DeductGFCModel archiWall, double tol, double angleTol)
        {
            Polygon bufferWall = strucWall.Outline;

            var isParallel = IsParallelWall(strucWall, archiWall, angleTol);
            if (isParallel)
            {
                var wallCenter = new List<Coordinate> { strucWall.CenterLine.P0, strucWall.CenterLine.P1 }.CreateLineString();
                var bufferInfo = new BufferOp(wallCenter, new BufferParameters()
                {
                    JoinStyle = JoinStyle.Mitre,
                    EndCapStyle = EndCapStyle.Flat,
                });

                var buffergeom = bufferInfo.GetResultGeometry(tol + strucWall.Width / 2);
                bufferWall = buffergeom.ToPolygon().FirstOrDefault();
            }

            return bufferWall;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="firstWall"></param>
        /// <param name="secondWall"></param>
        /// <param name="angleTol">radian</param>
        /// <returns></returns>
        public static bool IsParallelWall(DeductGFCModel firstWall, DeductGFCModel secondWall, double angleTol)
        {
            var isParallel = false;
            var swV = new Vector2D(firstWall.CenterLine.P0, firstWall.CenterLine.P1).Normalize();
            var awV = new Vector2D(secondWall.CenterLine.P0, secondWall.CenterLine.P1).Normalize();

            var angle = swV.Angle(awV);

            if (Math.Abs(Math.Cos(angle)) > Math.Cos(angleTol))
            {
                isParallel = true;
            }

            return isParallel;

        }

        public static List<DeductGFCModel> ToWallModel(DeductGFCModel oriWall, List<Polygon> newWallOutline)
        {
            var newWallModel = new List<DeductGFCModel>();

            for (int i = 0; i < newWallOutline.Count; i++)
            {
                var nWallModel = new DeductGFCModel();
                nWallModel.UID = System.Guid.NewGuid().ToString();
                nWallModel.Outline = newWallOutline[i];
                nWallModel.ZDir = oriWall.ZDir;
                nWallModel.ZValue = oriWall.ZValue;
                nWallModel.GlobalZ = oriWall.GlobalZ;
                nWallModel.ItemType = oriWall.ItemType;
                nWallModel.IFC = oriWall.IFC;
                nWallModel.CalculateWidthCLWallWidth(oriWall.Width);

                foreach (var pro in oriWall.Property)
                {
                    nWallModel.Property.Add(pro.Key, pro.Value);
                }

                //nWallModel.ChildItems.AddRange(oriWall.ChildItems);

                newWallModel.Add(nWallModel);
            }

            return newWallModel;
        }

        public static List<Polygon> SimplifyPl(this Polygon pl, double tol)
        {
            var plR = new List<Polygon>();

            var shrinkedGeomBuff = new BufferOp(pl, new BufferParameters()
            {
                JoinStyle = JoinStyle.Mitre,
            });
            var shrinkGeom = shrinkedGeomBuff.GetResultGeometry(-tol);
            var bufferGeomBuff = new BufferOp(shrinkGeom, new BufferParameters()
            {
                JoinStyle = JoinStyle.Mitre,
            });

            var bufferShrink = bufferGeomBuff.GetResultGeometry(tol);

            plR.AddRange(bufferShrink.ToPolygon());

            return plR;
        }

        private static List<Polygon> ToPolygon(this NetTopologySuite.Geometries.Geometry geom)
        {
            var objs = new List<Polygon>();
            if (geom is LineString lineString)
            {
                objs.Add(lineString.Coordinates.ToList().CreateLineString().CreatePolygon());
            }
            else if (geom is LinearRing linearRing)
            {
                objs.Add(linearRing.CreatePolygon());
            }
            else if (geom is Polygon polygon)
            {
                objs.Add(polygon);
            }
            else if (geom is MultiLineString lineStrings)
            {
                lineStrings.Geometries.ForEach(g => objs.AddRange(g.ToPolygon()));
            }
            else if (geom is MultiPolygon polygons)
            {
                polygons.Geometries.ForEach(g => objs.AddRange(g.ToPolygon()));
            }
            else if (geom is GeometryCollection geometries)
            {
                geometries.Geometries.ForEach(g => objs.AddRange(g.ToPolygon()));
            }

            return objs;
        }

    }
}
