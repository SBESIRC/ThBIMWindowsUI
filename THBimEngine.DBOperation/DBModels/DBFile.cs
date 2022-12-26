using SqlSugar;
using System;

namespace THBimEngine.DBOperation
{
    [SugarTable("FileUpload")]
    public class DBFile
    {
        public string FileId { get; set; }
        public string FileMD5 { get; set; }
        public string FileUrl { get; set; }
        public string FileRealName { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public string FileType { get; set; }
        [SugarColumn(IsIgnore = true)]
        public DateTime UploadTime { get; set; }
        /// <summary>
        /// 更新人Id
        /// </summary>
        public string UpdatedBy { get; set; } = null;
        /// <summary>
        /// 更新人名称
        /// </summary>
        public string UpdatedUserName { get; set; } = null;
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
        public int IsDel { get; set; }
    }
}
