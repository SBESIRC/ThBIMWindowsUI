using System;
using System.Collections.Generic;
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
        List<string> locations = new List<string>();
        bool autoLoginApp = false;
        bool autoOpenWindow = false;
        public UserInfo userInfo = null;
        public bool LoginSuccess { get { return userInfo != null; } }
        public Login(bool autoLogin = true,bool loginOpenWindow =true)
        {
            InitializeComponent();
            userInfo = null;
            userConfig = new UserConfig();
            InitLoacationInfo();
            cbxLocation.Items.Clear();
            foreach (var item in locations) 
            {
                cbxLocation.Items.Add(item);
            }
            InitDefaultValue();
            autoLoginApp = autoLogin;
            autoOpenWindow = loginOpenWindow;
            CheckAndAutoLogin();
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

        private void InitDefaultValue() 
        {
            bool isRememb = userConfig.Config.AppConfigBoolValue(userConfig.ConfigRemembPswKey);
            string uName = userConfig.Config.AppConfigStringValue(userConfig.ConfigUserName);
            string uPsw = userConfig.Config.AppConfigStringValue(userConfig.ConfigUPsw);
            string loaction = userConfig.Config.AppConfigStringValue(userConfig.ConfigSelectLocation);
            bool isAutoLogin = userConfig.Config.AppConfigBoolValue(userConfig.ConfigAutoLogin);
            if(!string.IsNullOrEmpty(loaction))
                cbxLocation.SelectedItem = loaction;
            if (cbxLocation.SelectedIndex < 0)
                cbxLocation.SelectedIndex = 0;
            ckbRemberPsw.IsChecked = isRememb;
            ckbAutoLogin.IsChecked = isAutoLogin;
            txtUName.Text = uName;
            txtUPsw.Password = uPsw;
        }
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string uName = txtUName.Text.ToString();
            string uPsw = txtUPsw.Password.ToString();
            UserLoginAndOpenMain(uName, uPsw);
        }

        private void ckbRemberPsw_Checked(object sender, RoutedEventArgs e)
        {
            userConfig.SetStringValue(userConfig.ConfigRemembPswKey, ckbRemberPsw.IsChecked == true?"True": "False");
        }

        private void ckbAutoLogin_Checked(object sender, RoutedEventArgs e)
        {
            userConfig.SetStringValue(userConfig.ConfigAutoLogin, ckbAutoLogin.IsChecked == true ? "True" : "False");
        }
        private void UserLoginAndOpenMain(string uName, string uPsw)
        {
            if (string.IsNullOrEmpty(uName) || string.IsNullOrEmpty(uPsw))
            {
                MessageBox.Show("用户名不能为空", "提醒", MessageBoxButton.OK);
                return;
            }
            userInfo = UserLogin(uName, uPsw, out string msg);
            if (null == userInfo)
            { 
                MessageBox.Show(string.Format("登录失败，{0}",msg),"操作提醒", MessageBoxButton.OK);
                return;
            }
            CheckAndOpenMain();
        }
        private UserInfo UserLogin(string uName,string uPsw,out string errorMsg) 
        {
            errorMsg = null;
            UserLoginService userLogin = new UserLoginService("TH3DViewer");
            try
            {
                userInfo = userLogin.UserLoginByNamePsw(uName, uPsw);
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                userInfo = null;
                return userInfo;
            }
            //保存登录的用户信息
            if (cbxLocation.SelectedIndex > -1)
                userInfo.LoginLocation = cbxLocation.SelectedItem.ToString();
            userConfig.SetStringValue(userConfig.ConfigUserName, uName);
            userConfig.SetStringValue(userConfig.ConfigSelectLocation, userInfo.LoginLocation);
            if (ckbRemberPsw.IsChecked == true)
            {
                userConfig.SetStringValue(userConfig.ConfigRemembPswKey, "True");
                userConfig.SetStringValue(userConfig.ConfigUPsw, uPsw);
            }
            else
            {
                userConfig.SetStringValue(userConfig.ConfigRemembPswKey, "False");
                userConfig.SetStringValue(userConfig.ConfigUPsw, "");
            }
            userConfig.SetStringValue(userConfig.ConfigAutoLogin, ckbAutoLogin.IsChecked == true ? "True" : "False");
            return userInfo;
        }
        private void CheckAndAutoLogin() 
        {
            //加载显示完成后，检查自动登录
            //本来应该放到Loaded事件或初始化中，但引擎运行时会报错
            if (!autoLoginApp || ckbAutoLogin.IsChecked != true)
                return;
            string uName = txtUName.Text.ToString();
            string uPsw = txtUPsw.Password.ToString();
            if (string.IsNullOrEmpty(uName) || string.IsNullOrEmpty(uPsw))
                return;
            UserLogin(uName, uPsw,out string msg);
        }
        public void CheckAndOpenMain() 
        {
            if (null == userInfo)
                return;
            if (!autoOpenWindow) 
            {
                this.Close();
                return;
            }
            XplorerMainWindow xplorer = new XplorerMainWindow(userInfo);
            this.Close();
            xplorer.Show();
            xplorer.RenderScene();
        }
    }


}
