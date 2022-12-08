using LBimFileCommit;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using THBimEngine.Common;

namespace THBimEngine.HttpService
{
    public class UploadFileToDB
    {
        string projectUrl = "http://81.69.195.121:8888/Portal/Api/ProejctController/GetProjects";
        string subProjectUrl = "http://81.69.195.121:8888/Portal/Api/ProejctController/GetSubentries";
        string projectFileUrl = "http://81.69.195.121:8888/Portal/Api/ProejctController/GetModelFiles";
        public List<DBProjectInfo> GetDBProject()
        {
            var dbProjects = new List<DBProjectInfo>();
            var client = new RestClient();
            var request = new RestRequest(new Uri(projectUrl, UriKind.RelativeOrAbsolute), Method.Get);
            request.RequestFormat = DataFormat.Json;
            var response = client.Execute(request);
            if (null != request && !string.IsNullOrEmpty(response.Content))
            {
                var res = JsonHelper.DeserializeJsonToObject<ResInfo>(response.Content);
                if (res.succ)
                    dbProjects = JsonHelper.DeserializeJsonToList<DBProjectInfo>(((Newtonsoft.Json.Linq.JContainer)res.Data).First().First().ToString());
            }
            return dbProjects;
        }
        public List<DBSubProjectInfo> GetSubProjects(string prjId) 
        {
            var dbSubProjects = new List<DBSubProjectInfo>();
            var client = new RestClient();
            var request = new RestRequest(subProjectUrl, Method.Get);
            request.AddParameter("prjId", prjId, ParameterType.QueryString,false);
            var response = client.Execute(request);
            if (null != request && !string.IsNullOrEmpty(response.Content))
            {
                try
                {
                    var res = JsonHelper.DeserializeJsonToObject<ResInfo>(response.Content);
                    if (res.succ)
                        dbSubProjects = JsonHelper.DeserializeJsonToList<DBSubProjectInfo>(((Newtonsoft.Json.Linq.JContainer)res.Data).First().First().ToString());
                }
                catch { }
            }
            return dbSubProjects;
        }
        public List<DBProjectFileInfo> GetSubProjectFiles(string subPrjId) 
        {
            var dbFiles = new List<DBProjectFileInfo>();
            var client = new RestClient();
            var request = new RestRequest(projectFileUrl, Method.Get);
            request.AddParameter("subId", subPrjId, ParameterType.QueryString, false);
            var response = client.Execute(request);
            if (null != request && !string.IsNullOrEmpty(response.Content))
            {
                try
                {
                    var res = JsonHelper.DeserializeJsonToObject<ResInfo>(response.Content);
                    if (res.succ)
                        dbFiles = JsonHelper.DeserializeJsonToList<DBProjectFileInfo>(((Newtonsoft.Json.Linq.JContainer)res.Data).First().First().ToString());
                }
                catch { }
            }
            return dbFiles;
        }

        public string FileUploadToDB(DBProjectInfo projectInfo, DBSubProjectInfo subProjectInfo, DBProjectFileInfo fileInfo, string filePath) 
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var args = new CommitArguments()
            {
                PrjId = projectInfo.Id,
                PrjName = projectInfo.PrjName,
                SubId = subProjectInfo.Id,
                SubName = subProjectInfo.SubEntryName,
                ModelId = fileInfo.ModelId,
                ModelName = fileName,
                FileId = fileInfo.Id,
                VerNo = fileInfo.VersionNo,
                ModelSubName = "默认",
                Major = MajorCodeToEMajor(fileInfo.Major),
                IfcFilePath = filePath,
                IsUpdate = false
            };
            var result = FileManager.Commit(args);
            return result.IsSucc ? "" : string.IsNullOrEmpty(result.ErrMessage) ? "上传失败" : result.ErrMessage;
        }
        public string FileUpdateToDB(DBProjectInfo projectInfo, DBSubProjectInfo subProjectInfo, DBProjectFileInfo fileInfo, string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var args = new CommitArguments()
            {
                PrjId = projectInfo.Id,
                PrjName = projectInfo.PrjName,
                SubId = subProjectInfo.Id,
                SubName = subProjectInfo.SubEntryName,
                ModelId = fileInfo.ModelId,
                ModelName = fileName,
                FileId = fileInfo.Id,
                VerNo = fileInfo.VersionNo,
                ModelSubName = "",
                Major = MajorCodeToEMajor(fileInfo.Major),
                IfcFilePath = filePath,
                IsUpdate = true
            };
            var result = FileManager.Commit(args);
            return result.IsSucc ? "" : string.IsNullOrEmpty(result.ErrMessage) ? "上传失败" : result.ErrMessage;
        }
        public MajorEnum MajorCodeToEMajor(string majorCode) 
        {
            var res = MajorEnum.建筑;
            switch (majorCode.ToUpper()) 
            {
                case "A":
                    res = MajorEnum.建筑;
                    break;
                case "S":
                    res = MajorEnum.结构;
                    break;
            }
            return res;
        }
    }
    public class DBProjectFileInfo 
    {
        public string Id { get; set; }
        public string ModelId { get; set; }
        public string FileName { get; set; }
        public string Major { get; set; }
        public string FileType { get; set; }
        public string DesignSubentryNames { get; set; }
        public string VersionNo { get; set; }
    }
    public class DBProjectInfo 
    {
        public string Id { get; set; }
        public string PrjNo { get; set; }
        public string PrjName { get; set; }
    }
    public class DBSubProjectInfo 
    {
        public string Id { get; set; }
        public string SubEntryName { get; set; }
    }
    public class ResInfo 
    {
        public bool succ { get; set; }
        public object err { get; set; }
        public object Data { get; set; }
    }
}
