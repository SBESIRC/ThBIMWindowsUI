﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace XbimXplorer.LeftTabItme.LeftTabControls
{
    /// <summary>
    /// MainFilterUControl.xaml 的交互逻辑
    /// </summary>
    public partial class MainFilterUControl : UserControl
    {
        public MainFilterUControl()
        {
            InitializeComponent();
           
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //gridMain.Visibility = Visibility.Collapsed;
            this.Visibility = Visibility.Collapsed;
        }
    }
}
