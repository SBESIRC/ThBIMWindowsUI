using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// MainFilterUControl.xaml 的交互逻辑
    /// </summary>
    public partial class MainFilterUControl : UserControl
    {
        public MainFilterUControl()
        {
            InitializeComponent();
            mainGrid.DataContext = FilterViewModel.Instance;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectItems = floorDGrid.SelectedItems.Cast<FloorFilterViewModel>();
            foreach (var item in FilterViewModel.Instance.AllFloorFilters) 
            {
                if (selectItems.Any(c => c == item))
                {
                    item.IsChecked = true;
                }
                else 
                {
                    item.IsChecked = false;
                }
            }
            FilterViewModel.Instance.UpdataFloorShowIds();
            FilterViewModel.Instance.ShowFilterResult();
        }

        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            floorDGrid.SelectAll();
        }
    }
}
