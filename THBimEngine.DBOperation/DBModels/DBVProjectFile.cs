using SqlSugar;
using System;

namespace THBimEngine.DBOperation.DBModels
{
    [SugarTable("FileUploadView")]
    public class DBVProjectFile
    {
        public string ProjectFileId { get; set; }
        public string ProjectUploadId { get; set; }
        public string FileId { get; set; }
        public string FileDownloadPath { get; set; }
        public string FileRealName { get; set; }
        public string FileMD5 { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public int IsMainFile { get; set; }
        public int CanOpen { get; set; }
        public int NeedDownload { get; set; }
        public DateTime UploadTime { get; set; }
    }


    [SugarTable("AllFileUploadView")]
    public class DBVAllProjectFile
    {
        public string ProjectFileId { get; set; }
        public string ProjectUploadId { get; set; }
        public string FileId { get; set; }
        public string FileDownloadPath { get; set; }
        public string FileRealName { get; set; }
        public string FileMD5 { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public int IsMainFile { get; set; }
        public int CanOpen { get; set; }
        public int NeedDownload { get; set; }
        public string VersionId { get; set; }
        public DateTime UploadTime { get; set; }
        public string IsDel { get; set; }
    }
}
