using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace THBimEngine.Application
{
    /// <summary>
    /// 文件打开参数
    /// </summary>
    public class ProjectParameter
    {
        /// <summary>
        /// 打开文件路径
        /// </summary>
        public string OpenFilePath { get; set; }
        /// <summary>
        /// 该文件对应的Id,外链Id,一个文件可以多次被外链（或项目中的Id）
        /// </summary>
        public string ProjectId { get; set; }
        /// <summary>
        /// 文件的偏移数据
        /// </summary>
        public XbimMatrix3D Matrix3D { get; set; }
        /// <summary>
        /// 项目的专业
        /// </summary>
        public EMajor Major { get; set; }
        /// <summary>
        /// 项目的来源先（如果CAD,SU,IFC,YDB）
        /// </summary>
        public EApplcationName Source { get; set; }
        /// <summary>
        /// 项目来源显示信息（如果CAD显示是主体）
        /// </summary>
        public string SourceShowName { get; set; }
        public ProjectParameter() 
        {
            Matrix3D = XbimMatrix3D.CreateTranslation(XbimVector3D.Zero);
        }
        public ProjectParameter(string filePath, EMajor major, EApplcationName applcationName) : this()
        {
            OpenFilePath = filePath;
            ProjectId = filePath;
            Major = major;
            Source = applcationName;
        }
    }
}
