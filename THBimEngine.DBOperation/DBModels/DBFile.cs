namespace THBimEngine.DBOperation
{
    public class DBFile
    {
        public int Id { get; set; }
        public string FileUrl { get; set; }
        public string FileRealName { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public string FileType { get; set; }
        public string UploadTime { get; set; }
        public string IsDel { get; set; }
    }
}
