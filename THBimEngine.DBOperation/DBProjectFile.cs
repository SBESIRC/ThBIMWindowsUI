using SqlSugar;
using System;
using System.Collections.Generic;

namespace THBimEngine.DBOperation
{
    [SugarTable("ProjectFile")]
    public class DBProjectFile
    {
        /// <summary>
        /// 项目文件记录Id
        /// </summary>
        public string ProjectFileId { get; set; }
        /// <summary>
        /// 项目Id
        /// </summary>
        public string PrjId { get; set; }
        /// <summary>
        /// 项目子项Id
        /// </summary>
        public string SubPrjId { get; set; }
        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 专业名称
        /// </summary>
        public string MajorName { get; set; }
        /// <summary>
        /// 来源信息
        /// </summary>
        public string ApplicationName { get; set; }
        /// <summary>
        /// 文件夹信息
        /// </summary>
        public string Folder { get; set; }
        /// <summary>
        /// 创建人Id
        /// </summary>
        public string CreaterId { get; set; }
        /// <summary>
        /// 创建人名称
        /// </summary>
        public string CreaterName { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 占用人
        /// </summary>
        public string Occupier { get; set; }
        /// <summary>
        /// 占用人名称
        /// </summary>
        public string OccupierName { get; set; }
        /// <summary>
        /// 是否删除(0,未删除，1已删除)
        /// </summary>
        public int IsDel { get; set; }
        [SugarColumn(IsIgnore = true)]
        public List<DBProjectFileUpload> FileUploads { get; set; }
    }
}
