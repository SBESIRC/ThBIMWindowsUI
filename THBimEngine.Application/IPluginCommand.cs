namespace THBimEngine.Application
{
    /// <summary>
    /// 顶部按钮继承的接口，按钮点击后，会调用Execute
    /// </summary>
    public interface IPluginCommand
    {
        void Execute(IEngineApplication engineApplication);
    }
    /// <summary>
    /// 左侧按钮，左侧是对应弹出UserControl，这里将信息绑定到页面
    /// </summary>
    public interface IPluginApplicaton
    {
        void BindApplication(IEngineApplication engineApplication);
    }
}
