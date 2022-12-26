using System.Windows;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// AddLinkSetUI.xaml 的交互逻辑
    /// </summary>
    public partial class AddLinkSetUI : Window
    {
        public AddLinkSetUI(double rotation,double moveX,double moveY,double moveZ,bool isAdd)
        {
            InitializeComponent();
            chbxNeedReturn.Visibility = isAdd ? Visibility.Visible : Visibility.Collapsed;
            txtRotation.Text = rotation.ToString();
            txtLocationX.Text = moveX.ToString();
            txtLocationY.Text = moveY.ToString();
            txtLocationZ.Text = moveZ.ToString();
        }
        public double GetInputData(out double moveX, out double moveY, out double moveZ,out bool needReturn) 
        {
            double rotation = TxtStringToDouble(txtRotation.Text.ToString());
            moveX = TxtStringToDouble(txtLocationX.Text.ToString());
            moveY = TxtStringToDouble(txtLocationY.Text.ToString());
            moveZ = TxtStringToDouble(txtLocationZ.Text.ToString());
            needReturn = chbxNeedReturn.IsChecked == true;
            return rotation;
        }
        private double TxtStringToDouble(string strTxt)
        {
            if (string.IsNullOrEmpty(strTxt))
                return 0.0;
            if (double.TryParse(strTxt, out double res))
                return res;
            return 0.0;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
