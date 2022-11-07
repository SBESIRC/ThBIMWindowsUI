using System;

namespace THBimEngine.DBOperation
{
    public class DBProjectFile
    {
        public int Id { get; set; }
        public string PrjNum { get; set; }
        public string SubPrjNum { get; set; }
        public string FileRealName { get; set; }
        public string MajorName { get; set; }
        public string SystemType { get; set; }
        public string Creater { get; set; }
        public string CreaterName { get; set; }
        public DateTime CreateTime { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public string IsDel { get; set; }
        public DBProjectFileUpload FileUpload { get; set; }
    }
}
