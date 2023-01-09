using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace XbimXplorer.Project
{
    /// <summary>
    /// ProjectFileHistory.xaml 的交互逻辑
    /// </summary>
    partial class ProjectFileHistory : Window
    {
        private bool haveChange = false;
        bool isBtnClose = false;
        FileHistoryVM fileHistoryVM;
        public ProjectFileHistory(List<FileHistory> showFiles,string showTitleName)
        {
            /*
             * 这里只记录修改后的数据，不直接保存数据到数据库，外面在根据这里处理后的数据保存数据库在进行相应的操作
             */
            InitializeComponent();
            haveChange = false;
            this.Title = showTitleName;
            fileHistoryVM = new FileHistoryVM(showFiles);
            this.DataContext = fileHistoryVM;
            cbxFilterName.Items.Add("全部");
            foreach (var item in fileHistoryVM.ShowFilterFileNames)
                cbxFilterName.Items.Add(item);
            cbxFilterName.SelectedIndex = 0;
        }
        public bool GetChangedFileInfos(out List<FileHistory> changeFileInfos) 
        {
            changeFileInfos = new List<FileHistory>();
            foreach (var item in fileHistoryVM.allFileHis)
            {
                if (item.OldMainFileUplaodId == item.NewMainFileUplaodId && item.NewState == item.State)
                    continue;
                changeFileInfos.Add(item);
            }
            return haveChange;
        }
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string filterName = "";
            if (cbxFilterName.SelectedIndex > 0) 
            {
                filterName = cbxFilterName.SelectedItem.ToString();
            }
            fileHistoryVM.FilterByFileName(filterName);
        }

        private void DataGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            DataGrid dGrid = (DataGrid)sender;
            if (dGrid == null)
                return;
            dGrid.ContextMenu = null;
            //这个时候datagrid的选中项还没有改变，不能通过选中项来处理
            // 获取坐标
            Point p = e.GetPosition((ItemsControl)sender);
            //  通过指定 Point 返回命中测试的最顶层 Visual 对象。                                               
            HitTestResult htr = VisualTreeHelper.HitTest((ItemsControl)sender, p);
            TextBlock o = htr.VisualHit as TextBlock;
            FileHistoryDetailVM fileHistory = null;
            if (o != null)
            {
                DataGridRow dgr = GetParentObject<DataGridRow>(o) as DataGridRow;
                fileHistory = dgr.DataContext as FileHistoryDetailVM;
                dgr.Focus();
                dgr.IsSelected = true;
            }
            if (null == fileHistory || !fileHistory.CanChange)
                return;
            if (fileHistory.NewState == "已作废")
            {
                ContextMenu aMenu = new ContextMenu();
                MenuItem syncMenu = new MenuItem();
                syncMenu.Header = "激活并使用该版本";
                syncMenu.Click += ActiveUseCurrent_Click;
                aMenu.Items.Add(syncMenu);
                dGrid.ContextMenu = aMenu;
            }
            else 
            {
                ContextMenu aMenu = new ContextMenu();
                MenuItem useMenu = new MenuItem();
                useMenu.Header = "使用该版本";
                useMenu.Click += UseCurrent_Click;
                aMenu.Items.Add(useMenu);
                dGrid.ContextMenu = aMenu;
            }
        }
        private void UseCurrent_Click(object sender, RoutedEventArgs e) 
        {
            var rowData = dGridHis.SelectedItem as FileHistoryDetailVM;
            if (null == rowData || rowData.IsCurrentVersion)
                return;
            fileHistoryVM.ProjectFileChangeUseVersion(rowData.ProjectFileId, rowData.ProjectFileUplaodId);
        }
        private void ActiveUseCurrent_Click(object sender, RoutedEventArgs e)
        {
            var rowData = dGridHis.SelectedItem as FileHistoryDetailVM;
            if (null == rowData)
                return;
            fileHistoryVM.ProjectFileActive(rowData.ProjectFileId, rowData.ProjectFileUplaodId);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            haveChange = CheckChange();
            isBtnClose = true;
            this.DialogResult = true;
            this.Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!CloseOrCancelHint())
                return;
            isBtnClose = true;
            this.DialogResult = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.DialogResult == true || isBtnClose)
                return;
            if (!CloseOrCancelHint())
                e.Cancel = true;
        }
        private bool CloseOrCancelHint() 
        {
            var isContinue = true;
            if (CheckChange())
            {
                string msg = "检查到数据有修改，未保存，如果取消这里的修改将会被丢弃（如果需要保存数据，请点击确认按钮）是否继续？";
                var res = MessageBox.Show(msg, "操作提醒", MessageBoxButton.YesNo, MessageBoxImage.Question);
                isContinue = res == MessageBoxResult.Yes;
            }
            return isContinue;
        }
        private bool CheckChange() 
        {
            var changed = false;
            foreach (var item in fileHistoryVM.allFileHis) 
            {
                if (changed)
                    break;
                if (item.OldMainFileUplaodId != item.NewMainFileUplaodId || item.NewState != item.State) 
                {
                    changed = true;
                    break;
                }
            }
            return changed;
        }
        private T GetParentObject<T>(DependencyObject obj) where T : FrameworkElement
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);

            while (parent != null)
            {
                if (parent is T)
                {
                    return (T)parent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
