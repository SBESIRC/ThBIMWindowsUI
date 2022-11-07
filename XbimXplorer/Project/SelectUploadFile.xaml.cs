using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace XbimXplorer
{
    /// <summary>
    /// SelectUploadFile.xaml 的交互逻辑
    /// </summary>
    public partial class SelectUploadFile : Window
    {
        List<string> majorNames;
        private string typeName = "";
        private bool isSelect;
        public SelectUploadFile(string type,List<string> majors,bool isSelectPath)
        {
            InitializeComponent();
            typeName = string.IsNullOrEmpty(type)?type: type.ToLower();
            majorNames = new List<string>();
            majorNames.AddRange(majors);
            foreach (var item in majors)
                comMajor.Items.Add(item);
            isSelect = isSelectPath;
            comMajor.SelectedIndex = 0;
            string titleName = "";
            if (isSelect) 
            {
                selectPath.Visibility = Visibility.Visible;
                selectPathLab.Visibility = Visibility.Visible;
                inputPath.Visibility = Visibility.Collapsed;
                labInput.Visibility = Visibility.Collapsed;
                titleName = "选择上传的 "+ type.ToUpper() + " 文件路径";
            }
            else
            {
                selectPath.Visibility = Visibility.Collapsed;
                selectPathLab.Visibility = Visibility.Collapsed;
                inputPath.Visibility = Visibility.Visible;
                labInput.Visibility = Visibility.Visible;
                titleName = "输入新建的 " + type.ToUpper()+" 文件名称";
            }
            this.Title = titleName;
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
        private void btnSelectPath_Click(object sender, RoutedEventArgs e)
        {
            SelectPathAndSetShow();
        }
        private void txtPath_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectPathAndSetShow();
        }
        public string GetSelectResult(out string major) 
        {
            major = comMajor.SelectedValue.ToString();
            if (isSelect)
                return txtPath.Text;
            else
                return inputPath.Text;
        }
        private void SelectPathAndSetShow() 
        {
            var selectPath = SelectFilePath();
            if (string.IsNullOrEmpty(selectPath))
                return;
            txtPath.Text = selectPath;
        }
        private string SelectFilePath()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = FilterStr()//"Text documents (.ifc)|*.ifc|All files (*.*)|*.*"
            };
            var result = openFileDialog.ShowDialog();
            if (result == true)
            {
                return openFileDialog.FileName;
            }
            else
            {
                return null;
            }
        }
        private string FilterStr() 
        {
            var filter = "文件选择(*.*)|*.*";
            if (string.IsNullOrEmpty(typeName))
                return filter;
            if (typeName == "ifc")
            {
                filter = "选择IFC文件(.ifc)|*.ifc";
            }
            else if (typeName == "ydb") 
            {
                filter = "选择IFC文件(.ydb)|*.ydb";
            }
            return filter;

        }
    }
}
