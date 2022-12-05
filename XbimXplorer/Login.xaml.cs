using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using THBimEngine.Domain;
using THBimEngine.HttpService;

namespace XbimXplorer
{
    /// <summary>
    /// Login.xaml 的交互逻辑
    /// </summary>
    public partial class Login : Window
    {
        UserConfig userConfig;
        string configRemembPswKey = "RemembPsw";
        string configUserName = "UserName";
        string configUPsw = "UserPsw";
        string configSelectLocation = "Local";
        string configUMajor = "Major";
        Dictionary<string, string> locationSQLIps = new Dictionary<string, string>();
        public Login()
        {
            InitializeComponent();
            var allName = EnumUtil.GetEnumDescriptions<EMajor>();
            comMajor.Items.Clear();
            foreach (var item in allName)
                comMajor.Items.Add(item);
            userConfig = new UserConfig();
            InitLoacationInfo();
            cbxLocation.Items.Clear();
            foreach (var item in locationSQLIps) 
            {
                cbxLocation.Items.Add(item.Key);
            }
            InitDefaultValue();
        }
        private void InitLoacationInfo() 
        {
            locationSQLIps = new Dictionary<string, string>();
            var appConfig = ConfigurationManager.AppSettings;
            var allKeys = appConfig.AllKeys;
            foreach (var item in allKeys) 
            {
                var upperKey = item.ToUpper();
                if (upperKey.StartsWith("LOCAL_")) 
                {
                    var showName = item.Substring(item.IndexOf("_") + 1);
                    if (locationSQLIps.ContainsKey(showName))
                        continue;
                    locationSQLIps.Add(showName, appConfig[item].ToString());
                }
            }
        }

        private void InitDefaultValue() 
        {
            bool isRememb = userConfig.Config.AppConfigBoolValue(configRemembPswKey);
            string uName = userConfig.Config.AppConfigStringValue(configUserName);
            string uPsw = userConfig.Config.AppConfigStringValue(configUPsw);
            string loaction = userConfig.Config.AppConfigStringValue(configSelectLocation);
            string major = userConfig.Config.AppConfigStringValue(configUMajor);
            if(!string.IsNullOrEmpty(loaction))
                cbxLocation.SelectedItem = loaction;
            if (cbxLocation.SelectedIndex < 0)
                cbxLocation.SelectedIndex = 0;
            if (!string.IsNullOrEmpty(major)) 
            {
                comMajor.SelectedItem = major;
            }
            if (comMajor.SelectedIndex < 0)
                comMajor.SelectedIndex = 0;
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
            UserInfo userInfo = null;
            string uName = txtUName.Text.ToString();
            string uPsw = txtUPsw.Password.ToString();
            try
            {
                mainWindow.IsEnabled = false;
                
                UserLoginService userLogin = new UserLoginService("TH3DViewer");
                userInfo = userLogin.UserLoginByNamePsw(uName, uPsw);
                userInfo.Majors = new List<string>();
                userInfo.Majors.Add(comMajor.SelectedItem.ToString());
                
                if (cbxLocation.SelectedIndex > -1)
                    userInfo.LoginLocation = cbxLocation.SelectedItem.ToString();
                //保存登录的用户信息
                userConfig.Config.UpdateOrAddAppConfig(configUserName, uName);
                userConfig.Config.UpdateOrAddAppConfig(configSelectLocation, userInfo.LoginLocation);
                userConfig.Config.UpdateOrAddAppConfig(configUMajor, string.Join(";", userInfo.Majors));
                if (!string.IsNullOrEmpty(userInfo.LoginLocation))
                {
                    userInfo.LocationSQLIp = locationSQLIps[userInfo.LoginLocation];
                }
                if (ckbRemberPsw.IsChecked == true)
                {
                    userConfig.Config.UpdateOrAddAppConfig(configRemembPswKey, "True");
                    userConfig.Config.UpdateOrAddAppConfig(configUPsw, uPsw);
                }
                else
                {
                    userConfig.Config.UpdateOrAddAppConfig(configRemembPswKey, "False");
                    userConfig.Config.UpdateOrAddAppConfig(configUPsw, "");
                }
            }
            catch (Exception ex)
            {
                userInfo = null;
                MessageBox.Show(ex.Message, "登录失败提醒", MessageBoxButton.OK);
                return;
            }
            finally
            {
                if (null != userInfo)
                {
                    XplorerMainWindow xplorer = new XplorerMainWindow(userInfo);
                    this.Close();
                    xplorer.Show();
                    xplorer.RenderScene();
                }
            }
            
        }
        private void ckbRemberPsw_Checked(object sender, RoutedEventArgs e)
        {
            if (ckbRemberPsw.IsChecked == true)
            {
                userConfig.Config.UpdateOrAddAppConfig(configRemembPswKey, "True");
            }
            else
            {
                userConfig.Config.UpdateOrAddAppConfig(configRemembPswKey, "False");
            }
        }
    }
}
