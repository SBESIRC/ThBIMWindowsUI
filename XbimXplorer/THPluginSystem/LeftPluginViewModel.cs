using System.Collections.ObjectModel;
using System.Windows.Controls;
using THBimEngine.Application;

namespace XbimXplorer.THPluginSystem
{
    class LeftPluginViewModel : NotifyPropertyChangedBase
    {
        public ObservableCollection<LeftTabItemBtn> LeftTabItems { get; set; }
        public LeftPluginViewModel(IEngineApplication engineApplication)
        {
            LeftTabItems = new ObservableCollection<LeftTabItemBtn>();
        }
    }
    class LeftTabItemBtn
    {
        public string ItemHead { get; }
        public double ItemPanelWidth { get; }
        public UserControl PanelControl { get; }
        public LeftTabItemBtn(string head, double width, UserControl control)
        {
            ItemHead = head;
            ItemPanelWidth = width;
            PanelControl = control;
        }
    }
}
