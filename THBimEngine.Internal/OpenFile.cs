using THBimEngine.Application;

namespace THBimEngine.Internal
{
    [EnginePlugin(PluginButtonType.Button, 0, "打开", "")]
    class OpenFile : IPluginCommand
    {
        public void Execute(IEngineApplication engineApplication)
        {
            var corefilters = new[] {
                "IFC Files|*.ifc;*.thbim;*.ydb",
                "Ifc File (*.ifc)|*.ifc",
                "thbim File (*.thbim)|*.thbim",
                "YJK File (*.ydb)|*.ydb"
            };
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = string.Join("|", corefilters)
            };
            var result = openFileDialog.ShowDialog();
            if (result != true)
                return;
            var filePath = openFileDialog.FileName;
            ProjectParameter openFileParameter = new ProjectParameter(filePath, Domain.EMajor.Structure, Domain.EApplcationName.IFC);
            engineApplication.LoadFileToCurrentDocument(openFileParameter);
        }
    }
}
