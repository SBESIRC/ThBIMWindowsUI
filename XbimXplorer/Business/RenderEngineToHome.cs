using THBimEngine.Application;

namespace XbimXplorer.Business
{
    [EnginePlugin(PluginButtonType.Button, 0, "Home", "")]
    class RenderEngineToHome : IPluginCommand
    {
        public void Execute(IEngineApplication engineApplication)
        {
            engineApplication.ZoomEntitys(null);
        }
    }
}
