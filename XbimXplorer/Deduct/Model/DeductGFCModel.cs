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
    internal class DeductGFCModel
    {
        public IfcProduct IFC { get; set; }//如果是新的话，这里放origin wall，uid再做新的
        public Polygon Outline { get; set; }
        public string UID { get; set; }
        public Vector3D ZDir { get; set; } = new Vector3D(0, 0, 1);
        public double ZValue { get; set; }//拉伸体：拉伸长度
        public double GlobalZ { get; set; }//全局标高，必须有，门窗的高度和墙不一样
        public DeductType ItemType { get; set; }
        
        /// <summary>
        /// 暂时给墙放内外属性用
        /// 以后可能放各种信息例如房间名称，材料等等
        /// </summary>
        public Dictionary<string, string> Property { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// 墙门窗贴面 厚度的缓存
        /// </summary>
        private double _width;
        public double Width
        {
            get
            {
                if (_width == -1)
                {
                    CalculateWidth();
                }
                return _width;
            }
            set
            {
                _width = value;
            }
        }

        /// <summary>
        /// 包含的uid
        /// 楼：层 
        /// 层：墙 门窗 房间 （天花板 地板 贴面）
        /// 墙:门窗 
        /// 贴面：NA(贴面和房间和门窗写在业务的映射
        /// 房间:天花板 地板 贴面（？）
        /// </summary>
        public List<string> ChildItems { get; private set; } = new List<string>();


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
            Outline = ifc.ToNTSPolygon();
            UID = ifc.GlobalId;
            ifc.GetExtrudedDepth(out var z, out var dir);
            ZValue = z;
            ZDir = dir;
            ItemType = GetDeductType(ifc, isArchi);
            ifc.GetGlobleZ(out var Zhight);
            GlobalZ = Zhight;

            _width = -1;
        }

        public double GetWidth()
        {
            if (Width == -1 && Outline != null)
            {
                CalculateWidth();
            }

            return Width;
        }

        private void CalculateWidth()
        {
            //根据outline计算
            Width = -1;
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
        Ceiling,
        Floor,//装修地面
        WallFaceFinish,//装修墙面
        Room,//房间
        Unknow,
    }
}
