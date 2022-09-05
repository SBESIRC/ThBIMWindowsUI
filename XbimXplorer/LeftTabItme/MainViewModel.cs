using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XbimXplorer.LeftTabItme.LeftTabControls;

namespace XbimXplorer.LeftTabItme
{
    class MainViewModel:NotifyPropertyChangedBase
    {
        public ObservableCollection<LeftTabItemBtn> LeftTabItems { get; set; }
        public MainViewModel() 
        {
            LeftTabItems = new ObservableCollection<LeftTabItemBtn>();
            LeftTabItems.Add(new LeftTabItemBtn("过\r\n滤", 300, new MainFilterUControl()));
        }
    }
}
