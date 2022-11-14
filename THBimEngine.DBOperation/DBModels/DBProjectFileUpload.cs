using SqlSugar;

namespace THBimEngine.DBOperation
{
    [SugarTable("AI_project")]
    public class DBProjectFileUpload
    {
        public string ProjectFileUploadId { get; set; }
        public string ProjectFileId { get; set; }
        public string FileName { get; set; }
        public string BuildingName { get; set; }
        public string FileUploadId { get; set; }
        public int IsDel { get; set; }
        [SugarColumn(IsIgnore = true)]
        public DBFile FileInfo { get; set; }
    }
}
