using SqlSugar;

namespace THBimEngine.DBOperation
{
    [SugarTable("FileUpload")]
    public class DBFile
    {
        public string UploadId { get; set; }
        public string FileMD5 { get; set; }
        public string FileUrl { get; set; }
        public string FileRealName { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public string FileType { get; set; }
        public string UploadTime { get; set; }
        public string IsDel { get; set; }
    }
}
