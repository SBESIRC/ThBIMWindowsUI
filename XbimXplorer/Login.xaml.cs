using System;
using System.Windows;
using THBimEngine.HttpService;

namespace XbimXplorer
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        public Login()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUName.Text) || string.IsNullOrEmpty(txtUPsw.Password))
            {
                MessageBox.Show("用户名不能为空", "提醒", MessageBoxButton.OK);
            }
            string uName = txtUName.Text.ToString();
            string uPsw = txtUPsw.Password.ToString();
            UserLoginService userLogin = new UserLoginService("TH3DViewer");
            UserInfo userInfo = null;
            try
            {
                userInfo = userLogin.UserLoginByNamePsw(uName, uPsw);
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message, "登录失败提醒", MessageBoxButton.OK);
                return;
            }
            XplorerMainWindow xplorer = new XplorerMainWindow(userInfo);
            this.Close();
            xplorer.Show();
        }
    }
}
