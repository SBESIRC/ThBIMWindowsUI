using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.DBOperation.DBModels
{
    [SugarTable("ProjectMainFileView")]
    public class DBVProjectMainFile
    {
        public string ProjectFileId { get; set; }
        public string PrjId { get; set; }
        public string PrjName { get; set; }
        public string SubPrjId { get; set; }
        public string SubPrjName { get; set; }
        public string FileName { get; set; }
        public string ApplicationName { get; set; }
        public string MajorName { get; set; }
        public string Occupier { get; set; }
        public string OccupierName { get; set; }
        public string CreaterId { get; set; }
        public string CreaterName { get; set; }
        public string CreateTime { get; set; }
        public string FileUploadId { get; set; }
    }

    [SugarTable("DeleteHistoryView")]
    public class DBVProjectMainDelFile
    {
        public string ProjectFileId { get; set; }
        public string PrjId { get; set; }
        public string PrjName { get; set; }
        public string SubPrjId { get; set; }
        public string SubPrjName { get; set; }
        public string FileName { get; set; }
        public string ApplicationName { get; set; }
        public string MajorName { get; set; }
    }

    [SugarTable("ProjectFileAllFiles")]
    public class DBVProjectAllFile
    {
        public string ProjectFileId { get; set; }
        public string ProjectFileUploadId { get; set; }
    }
}
