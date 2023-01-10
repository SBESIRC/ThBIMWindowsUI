using System.Collections.Generic;
using System.Windows;

namespace XbimXplorer
{
    /// <summary>
    /// ChangeLoginLocation.xaml 的交互逻辑
    /// </summary>
    public partial class ChangeLoginLocation : Window
    {
        List<string> locations = new List<string>();
        string selectLocation ="";
        public ChangeLoginLocation(string currentLocation)
        {
            InitializeComponent();
            labShowLocation.Content = string.Format("用户当前服务器【{0}】", currentLocation);
            InitLoacationInfo();
            int selectIndex = -1;
            int num = 0;
            foreach (var item in locations)
            {
                if (item == currentLocation)
                    selectIndex = num;
                listLocation.Items.Add(item);
                num += 1;
            }
            listLocation.SelectedIndex = selectIndex;
        }
        private void InitLoacationInfo()
        {
            locations = new List<string>();
            var allIpConfigs = IpConfigService.GetAllIpConfigs();
            foreach (ServiceIPConfig item in allIpConfigs)
            {
                locations.Add(item.ServiceName);
            }
        }
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            selectLocation = listLocation.SelectedItem.ToString();
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancle_Click(object sender, RoutedEventArgs e)
        {
            selectLocation = "";
            this.DialogResult = false;
            this.Close();
        }
        public string GetSelectLocation() 
        {
            return selectLocation;
        }
    }
}
