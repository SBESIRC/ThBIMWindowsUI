using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using THBimEngine.Application;

namespace XbimXplorer.THPluginSystem
{
    class PluginService
    {
        public List<PluginSvrCache> LeftPluginSvrCaches { get; }
        public List<PluginSvrCache> TopPluginSvrCaches { get; }
        object locker = new object();
        IEngineApplication engineApp;
        HashSet<string> hisDllPaths;
        public PluginService(IEngineApplication engineApplication, List<string> pluginDllPaths) 
        {
            LeftPluginSvrCaches = new List<PluginSvrCache>();
            TopPluginSvrCaches = new List<PluginSvrCache>();
            engineApp = engineApplication;
            hisDllPaths = new HashSet<string>();
            foreach (var item in pluginDllPaths) 
            {
                if (hisDllPaths.Contains(item))
                    continue;
                hisDllPaths.Add(item);
                CachePlugin(item);
            }
        }
        public void PluginAdd(string dllPath) 
        {
            if (!File.Exists(dllPath))
                return;
            CachePlugin(dllPath);
        }
        private void CachePlugin(string dllPath) 
        {
            try
            {
                var leftPlugins = GetPlugins(dllPath, out List<Type> btnTypes);
                if (leftPlugins.Count > 0)
                {
                    var tempLeftCache = new List<PluginSvrCache>();
                    foreach (var item in leftPlugins)
                    {
                        var temp = new PluginSvrCache(item);
                        tempLeftCache.Add(temp);
                    }
                    LeftPluginSvrCaches.AddRange(tempLeftCache.OrderBy(c => c.Order).ToList());
                }
                if (btnTypes.Count > 0)
                {
                    var tempTopCache = new List<PluginSvrCache>();
                    foreach (var item in btnTypes)
                    {
                        var temp = new PluginSvrCache(item);
                        tempTopCache.Add(temp);
                    }
                    TopPluginSvrCaches.AddRange(tempTopCache.OrderBy(c => c.Order).ToList());
                }
            }
            catch (Exception ex) 
            {
                throw ex;
            }
        }
        private List<Type> GetPlugins(string assembly,out List<Type> btnTypes) 
        {
            List<Type> leftCache = new List<Type>();
            btnTypes = new List<Type>();
            lock (locker) 
            {
                var ass = System.Reflection.Assembly.LoadFrom(assembly);
                leftCache = ass.GetTypes().Where(c=>c.GetInterfaces().Contains(typeof(IPluginApplicaton))
                    && c.GetCustomAttributes(typeof(EnginePluginAttribute),false).Length>0).ToList();
                btnTypes = ass.GetTypes().Where(c => c.GetInterfaces().Contains(typeof(IPluginCommand))
                     && c.GetCustomAttributes(typeof(EnginePluginAttribute), false).Length > 0).ToList();
            }
            return leftCache;
        }
    }
    class PluginSvrCache
    {
        public PluginButtonType ButtonType { get; }
        public int Order { get; }
        public string PluginName { get; }
        public string IconPath { get; }
        public Type PType { get; }
        public PluginSvrCache(Type type)
        {
            var attrs = type.GetCustomAttributes(typeof(EnginePluginAttribute), false);
            var attr = attrs.First(c => c is EnginePluginAttribute) as EnginePluginAttribute;
            this.Order = attr.Order;
            this.PluginName = attr.Content;
            this.IconPath = attr.IconPath;
            this.ButtonType = attr.ButtonType;
            this.PType = type;
        }
    }
}
