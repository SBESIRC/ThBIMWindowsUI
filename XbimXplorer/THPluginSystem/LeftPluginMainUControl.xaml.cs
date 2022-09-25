using System.Windows;
using System.Windows.Controls;

namespace XbimXplorer.THPluginSystem
{
    /// <summary>
    /// LeftPluginMainUControl.xaml 的交互逻辑
    /// </summary>
    public partial class LeftPluginMainUControl : UserControl
    {
        public LeftPluginMainUControl(UserControl child)
        {
            InitializeComponent();
            SetNewUControl(child);
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }
        public void SetNewUControl(UserControl child)
        {
            RemoveChildUserControl();
            mainGrid.Children.Add(child);
            this.Visibility = Visibility.Visible;
        }
        private void RemoveChildUserControl()
        {
            UserControl rmChild = null;
            foreach (var item in mainGrid.Children)
            {
                if (item is UserControl control)
                {
                    rmChild = control;
                    break;
                }
            }
            if (null != rmChild)
                mainGrid.Children.Remove(rmChild);
        }
    }
}
