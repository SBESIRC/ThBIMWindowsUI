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
        UserConfig userConfig;
        public Login()
        {
            InitializeComponent();
            userConfig = new UserConfig();
            bool isRememb = userConfig.Config.AppConfigBoolValue("RemembPsw");
            string uName = userConfig.Config.AppConfigStringValue("UserName");
            string uPsw = userConfig.Config.AppConfigStringValue("UserPsw");
            ckbRemberPsw.IsChecked = isRememb;
            txtUName.Text = uName;
            txtUPsw.Password = uPsw;
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
            //保存登录的用户信息
            userConfig.Config.UpdateOrAddAppConfig("UserName", uName);
            if (ckbRemberPsw.IsChecked == true)
            {
                userConfig.Config.UpdateOrAddAppConfig("RemembPsw", "True");
                userConfig.Config.UpdateOrAddAppConfig("UserPsw", uPsw);
            }
            else 
            {
                userConfig.Config.UpdateOrAddAppConfig("RemembPsw", "False");
                userConfig.Config.UpdateOrAddAppConfig("UserPsw", "");
            }
            XplorerMainWindow xplorer = new XplorerMainWindow(userInfo);
            this.Close();
            xplorer.Show();
        }

        private void ckbRemberPsw_Checked(object sender, RoutedEventArgs e)
        {
            if (ckbRemberPsw.IsChecked == true)
            {
                userConfig.Config.UpdateOrAddAppConfig("RemembPsw", "True");
            }
            else
            {
                userConfig.Config.UpdateOrAddAppConfig("RemembPsw", "False");
            }
        }
    }


}
