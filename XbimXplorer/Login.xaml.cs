﻿using System;
using System.Collections.Generic;
using System.Configuration;
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
        string configRemembPswKey = "RemembPsw";
        string configUserName = "UserName";
        string configUPsw = "UserPsw";
        string configSelectLocation = "Local";
        List<string> locations = new List<string>();
        public Login()
        {
            InitializeComponent();
            userConfig = new UserConfig();
            InitLoacationInfo();
            
            cbxLocation.Items.Clear();
            foreach (var item in locations) 
            {
                cbxLocation.Items.Add(item);
            }
            InitDefaultValue();
        }
        private void InitLoacationInfo() 
        {
            locations = new List<string>();
            var appConfig = ConfigurationManager.AppSettings;
            var allIpConfigs = IpConfigService.GetAllIpConfigs();
            foreach (ServiceIPConfig item in allIpConfigs) 
            {
                locations.Add(item.ServiceName);
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
            if (cbxLocation.SelectedIndex > -1)
                userInfo.LoginLocation = cbxLocation.SelectedItem.ToString();
            userConfig.Config.UpdateOrAddAppConfig(configUserName, uName);
            userConfig.Config.UpdateOrAddAppConfig(configSelectLocation, userInfo.LoginLocation);
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
