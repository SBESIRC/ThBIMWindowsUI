using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace XbimXplorer.LeftTabItme
{
    class LeftTabItemBtn
    {
        public string ItemHead { get; }
        public double ItemPanelWidth { get; }
        public UserControl PanelControl { get; }
        public LeftTabItemBtn(string head,double width,UserControl control) 
        {
            ItemHead = head;
            ItemPanelWidth = width;
            PanelControl = control;
        }
    }
}
