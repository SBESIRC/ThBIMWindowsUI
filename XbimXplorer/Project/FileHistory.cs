using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbimXplorer.Project
{
    public class FileHistory
    {
        public string MainFileId { get; set; }
        public string MainFileName { get; set; }
        public string OldMainFileUplaodId { get; set; }
        public string NewMainFileUplaodId { get; set; }
        public string State { get; set; }
        public string Occupier { get; set; }
        public string OccupierName { get; set; }
        public bool CanChange { get; set; }
        public string NewState { get; set; }
        public List<FileHistoryDetail> FileHistoryDetails { get; set; }
    }
    public class FileHistoryDetail 
    {
        public string ProjectFileId { get; set; }
        public string ProjectFileUplaodId { get; set; }
        public string ProjectFileUploadVersionId { get; set; }
        public DateTime FileUploadTime { get; set; }
        public string UploaderName { get; set; }
        public string ShowFileName { get; set; }
        public bool IsCurrentVersion { get; set; }
        public bool CanChange { get; set; }
        public string State { get; set; }
        public string NewState { get; set; }
    }
}
