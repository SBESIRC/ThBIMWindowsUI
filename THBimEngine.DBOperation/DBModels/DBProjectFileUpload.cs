using SqlSugar;

namespace THBimEngine.DBOperation
{
    [SugarTable("ProjectFileUpload")]
    public class DBProjectFileUpload
    {
        public string ProjectFileUploadId { get; set; }
        public string ProjectFileId { get; set; }
        public string FileName { get; set; }
        public string FileUploadId { get; set; }
        /// <summary>
        /// 是否是主文件（1主文件）
        /// </summary>
        public int IsMainFile { get; set; }
        /// <summary>
        /// 是否可以打开(1,可以打开)
        /// </summary>
        public int CanOpen { get; set; }
        public int NeedDownload { get; set; }
        /// <summary>
        /// 是否已删除
        /// </summary>
        public int IsDel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string VersionId { get; set; }
    }
}
