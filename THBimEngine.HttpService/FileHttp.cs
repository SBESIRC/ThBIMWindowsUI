using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace THBimEngine.HttpService
{
    public class FileHttp
    {
        string baseUrl = "";
        public FileHttp(string baseUrl) 
        {
            this.baseUrl = baseUrl;
            if (string.IsNullOrEmpty(baseUrl))
                this.baseUrl = "http://172.16.1.84:8090";
        }
        public bool UploadFile(string localPath, string newFileName, string folder) 
        {
            var client = new RestClient();
            var request = new RestRequest(baseUrl + "/FileUpdate/THBimWebService/FileUpdate", Method.Post);
            request.AddHeader("filename", HttpUtility.UrlEncode(newFileName,Encoding.UTF8) );
            request.AddHeader("filePath", HttpUtility.UrlEncode(folder, Encoding.UTF8));
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddFile(newFileName, localPath);
            client.Authenticator = new HttpBasicAuthenticator("upload", "Thape123123");
            var response = client.Execute(request);
            return response.Content.Contains("成功");
        }
        /*
        string baseUrl = "http://172.16.1.84:8085";
        public bool UploadFile(string localPath, string newFileName, string folder)
        {
            var client = new RestClient();
            var request = new RestRequest(baseUrl + "/FileUpdate/THBimWebService/FileUpdate", Method.Post);
            request.AddHeader("filename", HttpUtility.UrlEncode(newFileName, Encoding.UTF8));
            request.AddHeader("filePath", HttpUtility.UrlEncode(folder, Encoding.UTF8));
            request.AddHeader("Content-Type", "multipart/form-data");
            request.AddFile(newFileName, localPath);
            client.Authenticator = new HttpBasicAuthenticator("upload", "Thape123123");
            var response = client.Execute(request);
            return response.Content.Contains("成功");
        }*/
        public bool DownloadFile(string httpUrl, string loaclPath) 
        {
            bool success = false;
            try
            {
                if (File.Exists(loaclPath))
                {
                    File.Delete(loaclPath);
                }
                //url中/ # ? & % 空格有问题需要替换
                var tempUrl = httpUrl.Replace("/", "%2F");
                tempUrl = tempUrl.Replace("\\", "/");
                tempUrl = tempUrl.Replace("#", "%23");
                tempUrl = tempUrl.Replace("?", "%3F");
                tempUrl = tempUrl.Replace("&", "%26");
                tempUrl = tempUrl.Replace(" ", "%2B");
                string webPath = baseUrl + "/datas/" + tempUrl;
                using (WebClient client = new WebClient())
                {
                    client.Credentials = new NetworkCredential("upload", "Thape123123");
                    client.DownloadFile(webPath, loaclPath);
                }
                success = true;
            }
            catch
            {
                success = false;
            }
            finally 
            {
                if (!success) 
                {
                    if (File.Exists(loaclPath)) 
                    {
                        File.Delete(loaclPath);
                    }
                }
            }
            return success;
        }
    }
}
