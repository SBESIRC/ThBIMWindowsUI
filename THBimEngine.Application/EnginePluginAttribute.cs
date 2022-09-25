using System;

namespace THBimEngine.Application
{
    /// <summary>
    /// 按钮标识(左侧按钮是必须是UserControl)
    /// </summary>
    public class EnginePluginAttribute:Attribute
    {
        public string Content { get; }
        public string IconPath { get; }
        public int Order { get; }
        public PluginButtonType ButtonType { get; }
        public EnginePluginAttribute(PluginButtonType buttonType, int order, string text, string iconPath) 
        {
            Order = order;
            Content = text;
            IconPath = iconPath;
            ButtonType = buttonType;
        }
    }
    public enum PluginButtonType
    {
        Button =100,
        SplitButton =999,
    }
}
