using System;
using System.Collections.Generic;
using THBimEngine.Domain.GeometryModel;
using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public abstract class GeometryParam : ICloneable, IEquatable<GeometryParam>
    {
        public abstract object Clone();
        public abstract bool Equals(GeometryParam other);
    }

    /// <summary>
    /// 三维FacetedBrep数据
    /// </summary>
    public class GeometryFacetedBrep : GeometryParam
    {
        public List<ThTCHPolyline> Outer { get; }
        public List<ThTCHPolyline> Voids { get; }
        public GeometryFacetedBrep()
        {
            Outer = new List<ThTCHPolyline>();
            Voids = new List<ThTCHPolyline>();
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }

        public override bool Equals(GeometryParam other)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 二维轮廓拉伸几何信息
    /// </summary>
    public class GeometryStretch : GeometryParam
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
        public ThTCHPolyline Outline { get; set; }
        /// <summary>
        /// 降板外轮廓线
        /// </summary>
        public ThTCHPolyline OutlineBuffer { get; set; }
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
        public GeometryStretch(XbimPoint3D origin, XbimVector3D xAxis, double xAxisLength, double yAxisLength, XbimVector3D zAxis, double zAxisLength, double zOffSet = 0.0)
        {
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
        public GeometryStretch(PolylineSurrogate outline, XbimVector3D xAxis, XbimVector3D zAxis, double zAxisLength, double zOffSet = 0.0)
        {
            XAxis = xAxis;
            ZAxis = zAxis;
            ZAxisLength = zAxisLength;
            ZAxisOffSet = zOffSet;
            if (outline.InnerPolylines == null)
                outline.InnerPolylines = new List<PolylineSurrogate>();
            if (outline.Points == null)
                outline.Points = new List<Point3DCollectionSurrogate>();
            //Outline = outline;
        }

        /// <summary>
        /// 根据轮廓创建拉伸数据
        /// </summary>
        /// <param name="xAxis"></param>
        /// <param name="zAxis"></param>
        /// <param name="zAxisLength"></param>
        /// <param name="zOffSet"></param>
        public GeometryStretch(ThTCHPolyline outline, XbimVector3D xAxis, XbimVector3D zAxis, double zAxisLength, double zOffSet = 0.0)
        {
            XAxis = xAxis;
            ZAxis = zAxis;
            ZAxisLength = zAxisLength;
            ZAxisOffSet = zOffSet;
            //处理洞口的，现在暂时还没有处理
            //if (outline.InnerPolylines == null)
            //    outline.InnerPolylines = new List<PolylineSurrogate>();
            //if (outline.Points == null)
            //    outline.Points = new Google.Protobuf.Collections.RepeatedField<ThTCHPoint3d>();
            Outline = outline;
        }

        public override int GetHashCode()
        {
            return XAxisLength.GetHashCode()
                 ^ YAxisLength.GetHashCode()
                 ^ ZAxisLength.GetHashCode()
                 ^ Origin.GetHashCode()
                 ^ ZAxis.GetHashCode()
                 ^ XAxis.GetHashCode()
                 ^ ZAxisOffSet.GetHashCode();
        }

        public override bool Equals(GeometryParam other)
        {
            if (other is GeometryStretch geometry)
            {
                if (null != Outline && geometry.Outline != null)
                {
                    if (!Outline.Equals(geometry.Outline))
                        return false;
                }
                if (XAxisLength.FloatEquals(geometry.XAxisLength) &&
                    YAxisLength.FloatEquals(geometry.YAxisLength) &&
                    ZAxisLength.FloatEquals(geometry.ZAxisLength) &&
                    Origin.Equals(geometry.Origin) &&
                    ZAxis.Equals(geometry.ZAxis) &&
                    XAxis.Equals(geometry.XAxis) &&
                    ZAxisOffSet.Equals(geometry.ZAxisOffSet))
                    return true;
            }
            return false;
        }

        public override object Clone()
        {
            GeometryStretch clone = null;
            if (Outline != null && Outline.Points.Count > 0)
            {
                clone = new GeometryStretch(this.Outline, this.XAxis, this.ZAxis, this.ZAxisLength, this.ZAxisOffSet);
                clone.XAxisLength = this.XAxisLength;
                clone.YAxisLength = this.YAxisLength;
                clone.Origin = this.Origin;
                clone.OutlineBuffer = this.OutlineBuffer;
            }
            else
            {
                clone = new GeometryStretch(this.Origin, this.XAxis, this.XAxisLength, this.YAxisLength, this.ZAxis, this.ZAxisLength, this.ZAxisOffSet);
            }
            return clone;
        }
    }
}
