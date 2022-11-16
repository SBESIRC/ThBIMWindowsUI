using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.Common
{
    /// <summary>
    /// Config配置文件通用方法
    /// </summary>
    public class ConfigHelper
    {
        static Configuration config = null;

        //<summary>
        ///返回＊.exe.config文件中appSettings配置节的value项
        ///</summary>
        ///<param name="strKey"></param>
        ///<returns></returns>
        private static string GetAppConfig(string strKey)
        {
            foreach (string key in ConfigurationManager.AppSettings)
            {
                if (key == strKey)
                {
                    return ConfigurationManager.AppSettings[strKey];
                }
            }
            return null;
        }
        public ConfigHelper(string configPath)
        {
            if (string.IsNullOrEmpty(configPath))
                throw new Exception("无效的路径");
            if (!File.Exists(configPath))
                throw new Exception("配置文件不存在");
            try
            {
                ExeConfigurationFileMap exeConfiguration = new ExeConfigurationFileMap()
                {
                    ExeConfigFilename = configPath
                };
                config = ConfigurationManager.OpenMappedExeConfiguration(exeConfiguration, ConfigurationUserLevel.None);
            }
            catch (Exception ex) { throw ex; }
        }
        /// <summary>
        /// 更新或添加配置文件中的 appSettings节点
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        public void UpdateOrAddAppConfig(string key, string newValue)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            bool exist = false;
            foreach (string item in config.AppSettings.Settings.AllKeys)
                if (item == key)
                    exist = true;
            if (exist)
                config.AppSettings.Settings.Remove(key);
            config.AppSettings.Settings.Add(key, newValue);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
        #region 获取配置文件中的相应的节点
        /// <summary>
        /// 获取配置文件中的节点值 StringValue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string AppConfigStringValue(string key)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            string value = "";
            foreach (string item in config.AppSettings.Settings.AllKeys)
                if (item.Equals(key))
                    value = config.AppSettings.Settings[item].Value.ToString();
            return value;
        }
        /// <summary>
        /// 获取配置文件中的节点值 Bool
        /// 
        /// 没有找到时默认值为false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool AppConfigBoolValue(string key)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            bool value = false;
            string str = "";
            foreach (string item in config.AppSettings.Settings.AllKeys)
                if (item.Equals(key))
                    str = config.AppSettings.Settings[item].Value.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                str = str.ToUpper();
                value = str.Equals("TRUE");
            }
            return value;
        }
        /// <summary>
        /// 获取配置文件中的double值
        /// 
        /// 没有相应的节点或转换失败时 默认值double.MinValue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public double AppConfigDoubleValue(string key)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            double value = double.MinValue;
            string str = "";
            foreach (string item in config.AppSettings.Settings.AllKeys)
                if (item.Equals(key))
                    str = config.AppSettings.Settings[item].Value.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    value = Convert.ToDouble(str);
                }
                catch (Exception ex) { }
            }
            return value;
        }
        /// <summary>
        /// 获取配置文件中的Int值
        /// 
        /// 没有相应的节点或节点的值转换失败时，默认值int.MinValue
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int AppConfigIntValue(string key)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            int value = int.MinValue;
            string str = "";
            foreach (string item in config.AppSettings.Settings.AllKeys)
                if (item.Equals(key))
                    str = config.AppSettings.Settings[item].Value.ToString();
            if (!string.IsNullOrEmpty(str))
            {
                try
                {
                    value = Convert.ToInt32(str);
                }
                catch (Exception ex) { }
            }
            return value;
        }
        #endregion
        /// <summary>
        /// 更新或添加配置文件中的Connection节点
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        /// <param name="newProviderName"></param>
        public void UpdataOrAddConnectionConfig(string key, string newValue, string newProviderName)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key) || null == newValue)
                throw new Exception("传入的Key不能为空,传入的值不能为null");
            bool exist = false;
            if (config.ConnectionStrings.ConnectionStrings[key] != null)
                exist = true;
            if (exist)
                config.ConnectionStrings.ConnectionStrings.Remove(key);
            var keySettings = new ConnectionStringSettings(key, newValue, newProviderName);
            config.ConnectionStrings.ConnectionStrings.Add(keySettings);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("ConnectionStrings");
        }
        /// <summary>
        /// 更新或添加配置节点中的Connection节点
        /// </summary>
        /// <param name="key"></param>
        /// <param name="newValue"></param>
        public void UpdataOrAddConnectionConfig(string key, string newValue)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            bool exist = false;
            if (config.ConnectionStrings.ConnectionStrings[key] != null)
                exist = true;
            if (exist)
                config.ConnectionStrings.ConnectionStrings.Remove(key);
            var keySettings = new ConnectionStringSettings(key, newValue);
            config.ConnectionStrings.ConnectionStrings.Add(keySettings);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("ConnectionStrings");
        }
        /// <summary>
        /// 获取配置文件中的Connection节点的值 string
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string ConnectionConfigStringValue(string key)
        {
            if (null == config)
                throw new Exception("尚未初始化配置文件，无法进行操作");
            if (string.IsNullOrEmpty(key))
                throw new Exception("传入的Key不能为空");
            string value = config.ConnectionStrings.ConnectionStrings[key].ConnectionString.ToString();
            return value;
        }
    }
}
