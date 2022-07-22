using System.Collections.Generic;
using THBimEngine.Domain.Model.SurrogateModel;
using Xbim.Common.Geometry;
using System;
namespace THBimEngine.Domain
{
    public abstract class GeometryParam
    {
    }
    /// <summary>
    /// 二维轮廓拉伸几何信息
    /// </summary>
    public class GeometryStretch: GeometryParam, IEquatable<GeometryStretch>
    {
        /*
         二维轮廓通过拉伸方向和拉伸高度可以拉伸出一个3维几何体
        原点+法向 可以确定一个平面，也就是轮廓所在的平面
        构造二维轮廓的方式
        1、中心点+X轴+X轴长度+Y轴方向 矩形轮廓
        2、多边形轮廓，可能带洞口，List<多边形> 第一个为外轮廓，其余的为内轮廓（洞口）
         */
        #region 二维几何信息
        /// <summary>
        /// X轴方向长度（如果是多边形数据可以不给数据）
        /// </summary>
        public double XAxisLength { get; set; }
        /// <summary>
        /// Y轴方向长度（如果是多边形数据可以不给数据）
        /// </summary>
        public double YAxisLength { get; set; }
        /// <summary>
        /// Z轴方向拉伸长度
        /// </summary>
        public double ZAxisLength { get; set; }
        /// <summary>
        /// 面原点（如果是多边形数据可以不给数据）
        /// </summary>
        public XbimPoint3D Origin { get; set; }
        /// <summary>
        /// 面朝向（必须有，也是多边形拉伸方向）
        /// </summary>
        public XbimVector3D ZAxis { get; set; }
        /// <summary>
        /// X轴方向（必须有，和Z轴一起，可以确定坐标系）
        /// </summary>
        public XbimVector3D XAxis { get; set; }
        /// <summary>
        /// Z轴方向实体偏移量
        /// </summary>
        public double ZAxisOffSet { get; set; }
        /// <summary>
        /// 多边形轮廓（可以带洞口）第一个为外轮廓，其余的为内轮廓（洞口）
        /// </summary>
        public List<PolylineSurrogate> OutLine { get; private set; }
        #endregion
        /// <summary>
        /// 根据中心点创建矩形拉伸数据
        /// </summary>
        /// <param name="origin">中心点</param>
        /// <param name="xAxis">x轴方向</param>
        /// <param name="xAxisLength">x轴方向长度</param>
        /// <param name="yAxisLength">y轴方向长度</param>
        /// <param name="zAxis">z轴方向</param>
        /// <param name="zAxisLength">z轴方向拉伸长度</param>
        /// <param name="zOffSet">z轴偏移量</param>
        public GeometryStretch(XbimPoint3D origin, XbimVector3D xAxis, double xAxisLength,double yAxisLength, XbimVector3D zAxis,double zAxisLength,double zOffSet=0.0) 
        {
            Init();
            Origin = origin;
            XAxis = xAxis;
            XAxisLength = xAxisLength;
            YAxisLength = yAxisLength;
            ZAxis = zAxis;
            ZAxisLength = zAxisLength;
            ZAxisOffSet = zOffSet;
        }
        /// <summary>
        /// 根据轮廓创建拉伸数据
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="zAxis"></param>
        /// <param name="zAxisLength"></param>
        /// <param name="zOffSet"></param>
        public GeometryStretch(PolylineSurrogate polyline, XbimVector3D xAxis, XbimVector3D zAxis, double zAxisLength, double zOffSet = 0.0) 
        {
            Init();
            XAxis = xAxis;
            ZAxis = zAxis;
            ZAxisLength = zAxisLength;
            ZAxisOffSet = zOffSet;
            OutLine.Add(polyline);
        }
        private void Init() 
        {
            OutLine = new List<PolylineSurrogate>();
        }


        public override int GetHashCode()
        {
            return XAxisLength.GetHashCode() 
                 ^ YAxisLength.GetHashCode() 
                 ^ ZAxisLength.GetHashCode() 
                 ^ Origin.GetHashCode() 
                 ^ ZAxis.GetHashCode() 
                 ^ XAxis.GetHashCode() 
                 ^ ZAxisOffSet.GetHashCode()
                 ^ OutLine.Count;
        }

        public bool Equals(GeometryStretch other)
        {
            if (!OutLine.Count.Equals(other.OutLine.Count)) return false;
            for(int i = 0; i < OutLine.Count;i++)
            {
                if (!OutLine[i].Equals(other.OutLine[i])) return false;
            }
            if (XAxisLength.FloatEquals(other.XAxisLength) &&
                YAxisLength.FloatEquals(other.YAxisLength) &&
                ZAxisLength.FloatEquals(other.ZAxisLength) &&
                Origin.Equals(other.Origin) &&
                ZAxis.Equals(other.ZAxis) &&
                XAxis.Equals(other.XAxis) &&
                ZAxisOffSet.Equals(other.ZAxisOffSet))
                return true;
            return false;
        }
    }
}
