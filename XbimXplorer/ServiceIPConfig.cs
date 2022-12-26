using System.Collections.Generic;
using System.Configuration;

namespace XbimXplorer
{
    class ServiceAllIPSection: ConfigurationSection
    { 
        //fileUpload节点
        [ConfigurationProperty("ServiceIPItems")]
        public ServiceIPItemSection ServiceIPItems { get { return (ServiceIPItemSection)base["ServiceIPItems"]; } }
    }
    [ConfigurationCollection(typeof(ServiceIPConfig), AddItemName = "ServiceIP")]
    class ServiceIPItemSection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new ServiceIPConfig();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ServiceIPConfig)element).ServiceName;
        }

        public ServiceIPConfig this[int index]
        {
            get { return (ServiceIPConfig)base.BaseGet(index); }
        }

        new public ServiceIPConfig this[string name]
        {
            get { return (ServiceIPConfig)base.BaseGet(name); }
        }
    }
    class ServiceIPConfig:ConfigurationElement
    {
        //name属性
        [ConfigurationProperty("ServiceName", IsKey = true, IsRequired = true)]
        public string ServiceName { get { return (string)this["ServiceName"]; } set { ServiceName = value; } }
        //path属性
        [ConfigurationProperty("XTDBConnectString", IsRequired = true)]
        public string XTDBConnectString { get { return (string)this["XTDBConnectString"]; } set { XTDBConnectString = value; } }
        [ConfigurationProperty("DBConnectString", IsRequired = true)]
        public string DBConnectString { get { return (string)this["DBConnectString"]; } set { DBConnectString = value; } }
        [ConfigurationProperty("FileServiceIP", IsRequired = true)]
        public string FileServiceIP { get { return (string)this["FileServiceIP"]; } set { FileServiceIP = value; } }
    }
    class IpConfigService 
    {
        public static List<ServiceIPConfig> GetAllIpConfigs() 
        {
            List<ServiceIPConfig> ipConfigs = new List<ServiceIPConfig>();
            var allIpConfigs = (ServiceAllIPSection)ConfigurationManager.GetSection("ServiceAllIP");
            foreach (ServiceIPConfig item in allIpConfigs.ServiceIPItems)
            {
                ipConfigs.Add(item);
            }
            return ipConfigs;
        }
        public static ServiceIPConfig GetConfigByLocation(string location) 
        {
            var allIpConfigs = GetAllIpConfigs();
            foreach (ServiceIPConfig item in allIpConfigs)
            {
                if (item.ServiceName == location)
                    return item;
            }
            return null;
        }
    }
}
