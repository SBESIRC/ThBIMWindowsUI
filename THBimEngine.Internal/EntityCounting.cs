using System;
using System.Collections.Generic;
using System.Windows;
using THBimEngine.Application;
using THBimEngine.Domain;

namespace THBimEngine.Internal
{
    /*这是一个测试例子*/
    [EnginePlugin(PluginButtonType.Button, 0, "构件数量统计", "")]
    class EntityCounting : IPluginCommand
    {
        public void Execute(IEngineApplication engineApplication)
        {
            var currentDoc = engineApplication.CurrentDocument;
            if (null == currentDoc || currentDoc.AllBimProjects.Count < 1)
                return;
            Dictionary<Type, int> typeCount = new Dictionary<Type, int>();
            foreach (var project in currentDoc.AllBimProjects)
            {
                var res = project.ProjrctEntityTypeCounts();
                if (null == res || res.Count < 1)
                    continue;
                foreach (var keyValue in res)
                {
                    var key = keyValue.Key;
                    var keyCount = keyValue.Value.Count;
                    if (typeCount.ContainsKey(key))
                    {
                        typeCount[key] += keyCount;
                    }
                    else
                    {
                        typeCount.Add(key, keyCount);
                    }
                }
            }
            int sumCount = 0;
            foreach (var keyValue in typeCount)
            {
                var showTypeName = keyValue.Key.Name;
                var showCount = keyValue.Value;
                sumCount += showCount;
                engineApplication.Log.Info(string.Format("Type : {0} Count : {1}", showTypeName, showCount));
            }
            engineApplication.Log.Info(string.Format("Total Count : {0}", sumCount));
            MessageBox.Show("统计完成，请前往日志中查看结果", "提醒");
        }
    }
}
