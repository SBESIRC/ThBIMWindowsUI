using System.Windows;
using THBimEngine.Application;
using THBimEngine.Internal.UI;

namespace THBimEngine.Internal
{
    [EnginePlugin(PluginButtonType.Button, 1, "加解密", "")]
    class EncryptionFun : IPluginCommand
    {
        public void Execute(IEngineApplication engineApplication)
        {
            EncryptionUI encryptionUI = new EncryptionUI();
            encryptionUI.Owner = engineApplication as Window;
            encryptionUI.ShowDialog();
        }
    }
}
