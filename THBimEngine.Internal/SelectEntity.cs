using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Application;
using THBimEngine.Internal.UI;

namespace THBimEngine.Internal
{
    [EnginePlugin(PluginButtonType.Button, 2, "构件定位", "")]
    class SelectEntity : IPluginCommand
    {
        public void Execute(IEngineApplication engineApplication)
        {
            SelectEntityUI selectEntityUI = new SelectEntityUI(engineApplication);
            selectEntityUI.Show();
        }
    }
}
