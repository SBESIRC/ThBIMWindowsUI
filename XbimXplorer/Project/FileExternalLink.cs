using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Common;
using THBimEngine.Domain;

namespace XbimXplorer.Project
{
    public class FileExternalLink
    {
        public string LinkFilePath { get; }
        public string MainFilePath { get; }
        public List<LinkModel> LinkModels { get; set; }
        public FileExternalLink(string mainFilePath, string linkFilePath)
        {
            LinkFilePath = linkFilePath;
            MainFilePath = mainFilePath;
            LinkModels = new List<LinkModel>();
            var json = ReadFileData();
            if (string.IsNullOrEmpty(json))
                return;
            LinkModels = JsonHelper.DeserializeJsonToList<LinkModel>(json);
        }
        public void SaveToFile()
        {
            if (LinkModels.Count < 1)
                return;
            var jsonStr = JsonHelper.SerializeObject(LinkModels);
            FileStream fs = new FileStream(LinkFilePath, FileMode.Create);
            fs.Seek(0, SeekOrigin.Begin);
            fs.SetLength(0);
            using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
            {
                sw.WriteLine(jsonStr);
            }
        }
        string ReadFileData() 
        {
            if (string.IsNullOrEmpty(LinkFilePath) || !File.Exists(LinkFilePath))
                return "";
            string json = string.Empty;
            using (FileStream fs = new FileStream(LinkFilePath, FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    json = sr.ReadToEnd().ToString();
                }
            }
            return json;
        }
    }
}
