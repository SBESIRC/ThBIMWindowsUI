using System;
using System.Windows;
using THBimEngine.Common;

namespace THBimEngine.Internal.UI
{
    /// <summary>
    /// EncryptionUI.xaml 的交互逻辑
    /// </summary>
    public partial class EncryptionUI : Window
    {
        public EncryptionUI()
        {
            InitializeComponent();
        }

        private void BtnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            RSAEncDec(false);
        }

        private void BtnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            RSAEncDec(true);
        }
        private void RSAEncDec(bool isDec,string key="") 
        {
            var inputStr = txtStr.Text;
            if (string.IsNullOrEmpty(inputStr))
                return;
            try
            {
                if(isDec)
                    txtRes.Text = Encryption.AesDecrypt(inputStr, key);
                else
                    txtRes.Text = Encryption.AesEncrypt(inputStr, key);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            var inputStr = txtRes.Text;
            if (string.IsNullOrEmpty(inputStr))
                return;
            Clipboard.SetText(inputStr);
        }
        private void BtnClearRes_Click(object sender, RoutedEventArgs e)
        {
            txtRes.Text = "";
        }
        private void BtnClearAll_Click(object sender, RoutedEventArgs e)
        {
            txtStr.Text = "";
            txtRes.Text = "";
        }
    }
}
