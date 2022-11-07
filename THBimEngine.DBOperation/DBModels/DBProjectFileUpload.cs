using System;

namespace THBimEngine.DBOperation
{
    public class DBProjectFileUpload
    {
        public int Id { get; set; }
        public int PrjFileId { get; set; }
        public string FileName { get; set; }
        public DBFile FileInfo { get; set; }
        public DateTime UploadTime { get; set; }
        public string IsDel { get; set; }
    }
}
