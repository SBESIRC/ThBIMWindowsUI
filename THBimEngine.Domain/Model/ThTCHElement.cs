using ProtoBuf;
using System;
using System.Collections.Generic;
using THBimEngine.Domain.GeometryModel;

namespace THBimEngine.Domain.Model
{
    [ProtoContract]
    [ProtoInclude(100, typeof(ThTCHProject))]
    [ProtoInclude(101, typeof(ThTCHSite))]
    [ProtoInclude(102, typeof(ThTCHBuilding))]
    [ProtoInclude(103, typeof(ThTCHBuildingStorey))]
    [ProtoInclude(104, typeof(ThTCHWall))]
    [ProtoInclude(105, typeof(ThTCHDoor))]
    [ProtoInclude(106, typeof(ThTCHSlab))]
    [ProtoInclude(107, typeof(ThTCHWindow))]
    [ProtoInclude(108, typeof(ThTCHRailing))]
    [ProtoInclude(109, typeof(ThTCHOpening))]
    public class ThTCHElement
    {
        /*这里预留20个序列数据，外部序列数字从21开始*/
        [ProtoMember(1)]
        public string Name { get; set; }
        public string Useage { get; set; }
        [ProtoMember(2)]
        public string Uuid { get; set; }
        #region 几何信息
        [ProtoMember(3)]
        public PolylineSurrogate Outline { get; set; }
        [ProtoMember(4)]
        public Point3DSurrogate Origin { get; set; }
        //X轴方向和宽度方向一致
        [ProtoMember(5)]
        public Vector3DSurrogate XVector { get; set; }
        /// <summary>
        /// 宽度(厚度)（Y轴方向长度）
        /// </summary>
        [ProtoMember(6)]
        public double Width { get; set; }
        /// <summary>
        /// 长度(X轴方向)
        /// </summary>
        [ProtoMember(7)]
        public double Length { get; set; }
        /// <summary>
        /// 拉伸方向
        /// </summary>
        [ProtoMember(8)]
        public Vector3DSurrogate ExtrudedDirection { get; set; }
        /// <summary>
        /// 拉伸方向长度
        /// </summary>
        [ProtoMember(9)]
        public double Height { get; set; }
        /// <summary>
        /// 拉伸方向偏移值
        /// </summary>
        [ProtoMember(10)]
        public double ZOffSet { get; set; }
        [ProtoMember(11)]
        public string Material { get; set; }
        #endregion
        //传object数据有问题，后续需要处理
        [ProtoMember(19)]
        public Dictionary<string, string> Properties { get; set; }
        public ThTCHElement()
        {
            Uuid = Guid.NewGuid().ToString();
            Properties = new Dictionary<string, string>();
        }
    }
}
