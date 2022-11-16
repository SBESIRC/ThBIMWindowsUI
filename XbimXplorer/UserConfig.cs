using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Common;

namespace XbimXplorer
{
    class UserConfig
    {
        public ConfigHelper Config;
        string configPath = "";
        public UserConfig() 
        {
            var currentDir = Environment.CurrentDirectory;
            configPath = Path.Combine(currentDir, "userConfig.xml");
            InitConfigFile(configPath);
            Config = new ConfigHelper(configPath);
        }
        private void InitConfigFile(string filePath) 
        {
            if (File.Exists(filePath))
                return;
            //用户配置文件不存在，创建一个初始化的配置文件
            var create = File.Create(filePath);
            create.Close();
            var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            var writer = new StreamWriter(stream);
            writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            writer.WriteLine("<configuration>");
            writer.WriteLine("  <appSettings>");
            writer.WriteLine("    <add key=\"RemembPsw\" value=\"True\"/>");
            writer.WriteLine("    <add key=\"UserName\" value=\"\"/>");
            writer.WriteLine("    <add key=\"RemembPsw\" value=\"\"/>");
            writer.WriteLine("  </appSettings>");
            writer.WriteLine("</configuration>");
            writer.Close();
            stream.Close();

        }
    }
}
