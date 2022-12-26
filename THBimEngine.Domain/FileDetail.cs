using System;

namespace THBimEngine.Domain
{
    /// <summary>
    /// 项目文件的详细信息
    /// （一条项目文件记录，后面可能会有多个文件和其对应，如：ydb，对应的还有一个ifc和楼层信息的json文件）
    /// </summary>
    public class FileDetail:ICloneable
    {
        /// <summary>
        /// 项目主文件记录Id
        /// </summary>
        public string ProjectFileId { get; set; }
        /// <summary>
        /// 项目文件信息记录的Id(项目文件上传记录表中的Id)
        /// </summary>
        public string ProjectUploadId { get; set; }
        /// <summary>
        /// 项目文件对应文件上传记录的Id
        /// </summary>
        public string FileId { get; set; }
        /// <summary>
        /// 项目文件对应的本地完整路径
        /// </summary>
        public string FileLocalPath { get; set; }
        /// <summary>
        /// 文件的下载路径
        /// </summary>
        public string FileDownloadPath { get; set; }
        /// <summary>
        /// 文件的真实名称（下载路径中的文件名为guid名称），下载时会重命名为真实名称
        /// </summary>
        public string FileRealName { get; set; }
        /// <summary>
        /// 文件的MD5 检验使用
        /// </summary>
        public string FileMD5 { get; set; }
        /// <summary>
        /// 上传人Id
        /// </summary>
        public string Uploader { get; set; }
        /// <summary>
        /// 上传人用户名
        /// </summary>
        public string UploaderName { get; set; }
        /// <summary>
        /// 是否是主文件（如YDB，skp文件）
        /// </summary>
        public bool IsMainFile { get; set; }
        /// <summary>
        /// 文件是否可以打开（ifc文件可以直接打开）
        /// </summary>
        public bool CanOpen { get; set; }
        /// <summary>
        /// 是否需要下载到本地，（如ydb文件，上传到服务器后，可以不再下载，只需要下载对应的ifc文件就可以进行载入）
        /// </summary>
        public bool NeedDownload { get; set; }
        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UploadTime { get; set; }

        public object Clone()
        {
            var clone = new FileDetail();
            clone.IsMainFile = this.IsMainFile;
            clone.UploadTime = this.UploadTime;
            clone.NeedDownload = this.NeedDownload;
            clone.CanOpen = this.CanOpen;
            clone.ProjectFileId = this.ProjectFileId;
            clone.ProjectUploadId = this.ProjectUploadId;
            clone.FileId = this.FileId;
            clone.FileLocalPath = this.FileLocalPath;
            clone.FileDownloadPath = this.FileDownloadPath;
            clone.FileRealName = this.FileRealName;
            clone.FileMD5 = this.FileMD5;
            clone.Uploader = this.Uploader;
            clone.UploaderName = this.UploaderName;
            return clone;
        }
    }
}
