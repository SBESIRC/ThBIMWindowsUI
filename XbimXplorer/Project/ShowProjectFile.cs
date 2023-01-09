using System;
using System.Collections.Generic;
using THBimEngine.Domain;

namespace XbimXplorer
{
    public class ShowProjectFile : ShortProjectFile
    {
        public string ShowFileName { get; set; }
        public string ShowSourceName { get; set; }
        public DateTime LastUpdateTime { get; set; }
        public FileDetail MainFile { get; set; }
        public string OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OccupyId { get; set; }
        public string OccupyName { get; set; }
        public string Uploader { get; set; }
        public string UploaderName { get; set; }
        public FileDetail OpenFile { get; set; }
        public bool CanLink
        {
            get { return OpenFile != null; }
        }
        public List<FileDetail> FileInfos { get; set; }
        //外链的模型为了保持最新，这里缓存数据在双击时自动刷新
        public List<ShowFileLink> FileLinks { get; set; }
    }
}
