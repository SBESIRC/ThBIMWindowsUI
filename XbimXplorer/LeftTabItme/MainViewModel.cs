using System.Collections.ObjectModel;
using Xbim.Presentation.XplorerPluginSystem;
using XbimXplorer.LeftTabItme.LeftTabControls;

namespace XbimXplorer.LeftTabItme
{
    class MainViewModel:NotifyPropertyChangedBase
    {
        public ObservableCollection<LeftTabItemBtn> LeftTabItems { get; set; }
        public MainViewModel(IXbimXplorerPluginMasterWindow xbimXplorer) 
        {
            LeftTabItems = new ObservableCollection<LeftTabItemBtn>();
            LeftTabItems.Add(new LeftTabItemBtn("过\r\n滤", 300, new MainFilterUControl()));
            var tempUControl =new LinkUControl();
            tempUControl.BindUi(xbimXplorer);
            LeftTabItems.Add(new LeftTabItemBtn("外\r\n链", 300, tempUControl));
        }
    }
}
