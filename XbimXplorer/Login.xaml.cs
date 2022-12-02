using System;
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
        List<string> majorNames;
        string configRemembPswKey = "RemembPsw";
        string configUserName = "UserName";
        string configUPsw = "UserPsw";
        string configSelectLocation = "Local";
        Dictionary<string, string> locationSQLIps = new Dictionary<string, string>();
        public Login()
        {
            InitializeComponent();
            majorNames = new List<string>();
            var allName = EnumUtil.GetEnumDescriptions<EMajor>();
            majorNames.AddRange(allName);
            comMajor.Items.Clear();
            foreach (var item in majorNames)
                comMajor.Items.Add(item);
            userConfig = new UserConfig();
            bool isRememb = userConfig.Config.AppConfigBoolValue("RemembPsw");
            string uName = userConfig.Config.AppConfigStringValue("UserName");
            string uPsw = userConfig.Config.AppConfigStringValue("UserPsw");
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
            if(!string.IsNullOrEmpty(loaction))
                cbxLocation.SelectedItem = loaction;
            if (cbxLocation.SelectedIndex < 0)
                cbxLocation.SelectedIndex = 0;
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
            try
            {
                mainWindow.IsEnabled = false;
                string uName = txtUName.Text.ToString();
                string uPsw = txtUPsw.Password.ToString();
                UserLoginService userLogin = new UserLoginService("TH3DViewer");
                userInfo = userLogin.UserLoginByNamePsw(uName, uPsw);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "登录失败提醒", MessageBoxButton.OK);
                return;
            }
            //保存登录的用户信息
            if (cbxLocation.SelectedIndex > -1)
                userInfo.LoginLocation = cbxLocation.SelectedItem.ToString();
            userConfig.Config.UpdateOrAddAppConfig(configUserName, uName);
            userConfig.Config.UpdateOrAddAppConfig(configSelectLocation, userInfo.LoginLocation);
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
            XplorerMainWindow xplorer = new XplorerMainWindow(userInfo);
            this.Close();
            xplorer.Show();
            xplorer.RenderScene();
        }
                userInfo.Majors = new List<string>();
                userInfo.Majors.Add(comMajor.SelectedItem.ToString());
                //保存登录的用户信息
                userConfig.Config.UpdateOrAddAppConfig("UserName", uName);
                userConfig.Config.UpdateOrAddAppConfig("Major", string.Join(";", userInfo.Majors));
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
                if (cbxLocation.SelectedIndex > -1)
                    userInfo.LoginLocation = cbxLocation.SelectedItem.ToString();
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
