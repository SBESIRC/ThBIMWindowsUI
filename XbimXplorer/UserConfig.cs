using System;
using System.IO;
using THBimEngine.Common;

namespace XbimXplorer
{
    class UserConfig
    {
        public ConfigHelper Config;
        public string ConfigRemembPswKey = "RemembPsw";
        public string ConfigUserName = "UserName";
        public string ConfigUPsw = "UserPsw";
        public string ConfigSelectLocation = "Local";
        public string ConfigAutoLogin = "AutoLogin";
        string configPath = "";
        public UserConfig() 
        {
            var currentDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);//Environment.CurrentDirectory;
            currentDir = Path.Combine(currentDir, "thbim");
            configPath = Path.Combine(currentDir, "userConfig.xml");
            if (!Directory.Exists(currentDir))
                Directory.CreateDirectory(currentDir);
            InitConfigFile(configPath);
            Config = new ConfigHelper(configPath);
        }
        public void SetStringValue(string key,string value) 
        {
            Config.UpdateOrAddAppConfig(key, value);
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
            writer.WriteLine("    <add key=\"UserPsw\" value=\"\"/>");
            writer.WriteLine("    <add key=\"Local\" value=\"\"/>");
            writer.WriteLine("    <add key=\"AutoLogin\" value=\"True\"/>");
            writer.WriteLine("  </appSettings>");
            writer.WriteLine("</configuration>");
            writer.Close();
            stream.Close();

        }
    }
}
