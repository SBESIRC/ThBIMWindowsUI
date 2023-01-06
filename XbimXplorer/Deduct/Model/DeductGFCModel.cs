using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xbim.Ifc2x3.Kernel;
using NetTopologySuite.Geometries;
using NetTopologySuite.Mathematics;
using ThBIMServer.NTS;

namespace XbimXplorer.Deduct.Model
{
    public class DeductGFCModel
    {
        public IfcProduct IFC { get; set; }//如果是新的话，这里放origin wall，uid再做新的
        public Polygon Outline { get; set; }
        public string UID { get; set; }
        public Vector3D ZDir { get; set; } = new Vector3D(0, 0, 1);
        public double ZValue { get; set; }//拉伸体：拉伸长度
        public double GlobalZ { get; set; }//全局标高，必须有，门窗的高度和墙不一样
        public DeductType ItemType { get; set; }
        /// <summary>
        /// 包含的uid
        /// 楼：层 
        /// 层：墙 门窗 房间 （天花板 地板 贴面）
        /// 墙:门窗 
        /// 贴面：NA(贴面和房间和门窗写在业务的映射
        /// 房间:天棚 地板 贴面（？）
        /// </summary>
        public List<string> ChildItems { get; private set; } = new List<string>();

        /// <summary>
        /// 暂时给墙放内外属性用
        /// 以后可能放各种信息例如房间名称，材料等等
        /// </summary>
        public Dictionary<string, string> Property { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// 墙门窗贴面 厚度的缓存
        /// 楼板，房间，天棚，地板不适用！
        /// </summary>
        private double _width;
        public double Width
        {
            get
            {
                if (_width == 0)
                {
                    //初始值0， 计算过一次如果出错就是-1
                    CalculateWidthCenterLineNew();
                }
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        private LineSegment _CenterLine;
        public LineSegment CenterLine
        {
            get
            {
                if (_CenterLine == null)
                {
                    //初始值null， 计算过一次如果出错就是length=0的line
                    CalculateWidthCenterLineNew();
                }
                return _CenterLine;
            }
            set
            {
                _CenterLine = value;
            }
        }


        public DeductGFCModel()
        {

        }

        /// <summary>
        /// 墙门窗等拉伸体用，building， storey 没有实体几何不要用
        /// </summary>
        /// <param name="ifc"></param>
        /// <param name="isArchi"></param>
        public DeductGFCModel(IfcProduct ifc, bool isArchi)
        {
            IFC = ifc;
            UID = ifc.GlobalId;
            ItemType = GetDeductType(ifc, isArchi);
            //Outline = ifc.ToNTSPolygon();
            //ifc.GetExtrudedDepth(out var z, out var dir);
            //ZValue = z;
            //ZDir = dir;
            //ifc.GetGlobleZ(out var Zhight);
            //GlobalZ = Zhight;

            var ifcInfo = ifc.AnalyzeIfcProduct();
            if (ItemType == DeductType.ArchiWall || ItemType == DeductType.StructWall ||
                ItemType == DeductType.Door || ItemType == DeductType.Window)
            {
                Outline = ifcInfo.Item1.ToObb();
            }
            else
            {
                Outline = ifcInfo.Item1;
            }


            ZValue = ifcInfo.Item2;
            GlobalZ = ifcInfo.Item3;
            ZDir = new Vector3D(0, 0, 1);

            _width = 0;
        }
        private void CalculateWidthCenterLine()
        {
            //根据outline计算
            var geom = Outline;
            Coordinate stPt = new Coordinate();
            Coordinate endPt = new Coordinate();
            CenterLine = new LineSegment(stPt, endPt);
            Width = -1;

            if (geom != null && geom.Shell != null && Width != -2)
            {
                var pts = geom.Shell.Coordinates;
                var pt0 = new Coordinate();
                var pt1 = new Coordinate();
                var pt2 = new Coordinate();
                var pt3 = new Coordinate();

                if (pts.Count() >= 4)
                {
                    pt0 = pts[0];
                    pt1 = pts[1];
                    pt2 = pts[2];
                    pt3 = pts[3];

                    if (pt0.Distance(pt1) <= pt1.Distance(pt2))
                    {
                        stPt = pt0.GetCenter(pt1);
                        endPt = pt2.GetCenter(pt3);
                        Width = pt0.Distance(pt1);

                    }
                    else
                    {
                        stPt = pt1.GetCenter(pt2);
                        endPt = pt3.GetCenter(pt0);
                        Width = pt1.Distance(pt2);
                    }
                    CenterLine = new LineSegment(stPt, endPt);
                }
                else if (pts.Count() == 3)
                {
                    pt0 = pts[0];
                    pt1 = pts[1];

                    stPt = pt0;
                    endPt = pt1;

                    Width = pt0.Distance(pt1);
                    CenterLine = new LineSegment(stPt, endPt);
                }
            }
        }


        private void CalculateWidthCenterLineNew()
        {
            if (ItemType == DeductType.ArchiWall || ItemType == DeductType.StructWall)
            {
                CalculateWidthCLWallWidth();
            }
            else if (ItemType == DeductType.Door || ItemType == DeductType.Window)
            {
                CalculateWidthCLShortEdge();
            }
            else
            {
                InitialWidth();
            }

        }
        /// <summary>
        /// 根据短边优先级
        /// 会强制转obb为0>1短边
        /// </summary>
        private void CalculateWidthCLShortEdge()
        {
            //根据outline计算
            InitialWidth();

            if (Outline != null && Outline.Shell != null)
            {
                var pts = Outline.Shell.Coordinates;
                if (pts.Count() >= 4)
                {
                    if (pts[0].Distance(pts[1]) > pts[1].Distance(pts[2]))
                    {
                        var newPts = TurnPts(pts.ToList(), 1);
                        Outline = newPts.CreateLineString().CreatePolygon();
                    }
                    SetWidth();
                }
            }
        }

        /// <summary>
        /// 墙厚度优先级 给定厚度 > 200 >100 >短边
        /// </summary>
        public void CalculateWidthCLWallWidth(double WallWidth = -1)
        {
            //根据outline计算
            InitialWidth();

            if (Outline != null && Outline.Shell != null)
            {
                var pts = Outline.Shell.Coordinates;
                var ptIdx = -1;

                if (WallWidth != -1 && ptIdx == -1)
                {
                    ptIdx = CheckOutlineEdge(pts, WallWidth);
                }
                if (ptIdx == -1)
                {
                    ptIdx = CheckOutlineEdge(pts, 200);
                }
                if (ptIdx == -1)
                {
                    ptIdx = CheckOutlineEdge(pts, 100);
                }
                if (ptIdx == -1)
                {
                    CalculateWidthCLShortEdge();
                }
                else
                {
                    if (ptIdx != 0)
                    {
                        var newPts = TurnPts(pts.ToList(), ptIdx);
                        Outline = newPts.CreateLineString().CreatePolygon();
                    }
                    SetWidth();
                }
            }
        }
        private void InitialWidth()
        {
            var stPt = new Coordinate();
            var endPt = new Coordinate();
            CenterLine = new LineSegment(stPt, endPt);
            Width = -1;
        }
        private void SetWidth()
        {
            var pts = Outline.Shell.Coordinates;
            var stPt = pts[0].GetCenter(pts[1]);
            var endPt = pts[2].GetCenter(pts[3]);

            Width = pts[0].Distance(pts[1]);
            CenterLine = new LineSegment(stPt, endPt);
        }
        private static List<Coordinate> TurnPts(List<Coordinate> pts, int turn)
        {
            var ptsNew = new List<Coordinate>();

            if (pts.Last().Distance(pts.First()) < 1)
            {
                pts.RemoveAt(pts.Count() - 1);
            }

            ptsNew.AddRange(pts);

            if (turn != 0)
            {
                for (int i = 0; i < ptsNew.Count(); i++)
                {
                    ptsNew[i] = pts[(i + turn) % pts.Count()];
                }
                ptsNew.Add(ptsNew.First());
            }
            return ptsNew;
        }
        private static int CheckOutlineEdge(Coordinate[] pts, double wallWidth)
        {
            var ptIdx = -1;
            for (int i = 0; i < pts.Count() - 1; i++)
            {
                var dist = pts[i].Distance(pts[i + 1]);
                if (Math.Abs(dist - wallWidth) < 1)
                {
                    ptIdx = i;
                    break;
                }
            }

            return ptIdx;
        }

        public static DeductType GetDeductType(IfcProduct IFC, bool isArchi = false)
        {
            var dt = DeductType.Unknow;
            if (IFC != null)
            {
                var typeIFC = IFC.GetType().Name.ToString().ToUpper();
                switch (typeIFC)
                {
                    case "IFCWALL":

                        if (isArchi)
                        {
                            dt = DeductType.ArchiWall;
                        }
                        else
                        {
                            dt = DeductType.StructWall;
                        }
                        break;

                    case "IFCDOOR":
                        dt = DeductType.Door;
                        break;

                    case "IFCWINDOW":
                        dt = DeductType.Window;
                        break;
                    case "IFCSPACE":
                        dt = DeductType.Room;
                        break;

                    case "IFCBUILDINGSTOREY":
                        if (isArchi)
                        {
                            dt = DeductType.ArchiStorey;
                        }
                        else
                        {
                            dt = DeductType.StructStorey;
                        }
                        break;

                    case "IFCBUILDING":
                        dt = DeductType.Building;
                        break;

                    case "IFCSLAB":
                        dt = DeductType.Slab;
                        break;
                    default:

                        break;
                }

            }
            return dt;
        }
    }

    public enum DeductType
    {
        Building,
        ArchiStorey,
        StructStorey,
        ArchiWall,
        StructWall,
        Door,
        Window,
        Slab,
        Ceiling,
        Floor,//装修地面
        WallFaceFinish,//装修墙面
        Room,//房间
        Unknow,
    }
}
