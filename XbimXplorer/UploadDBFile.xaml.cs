using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using THBimEngine.Application;
using THBimEngine.HttpService;

namespace XbimXplorer
{
    /// <summary>
    /// UploadDBFile.xaml 的交互逻辑
    /// </summary>
    public partial class UploadDBFile : Window
    {
        SelectUploadFileVM uploadFileVM;
        public UploadDBFile(List<DBProjectFileInfo> projectFileInfos)
        {
            InitializeComponent();
            uploadFileVM = new SelectUploadFileVM(projectFileInfos);
            this.DataContext = uploadFileVM;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        public DBProjectFileInfo GetSelectFile() 
        {
            if (null == uploadFileVM || uploadFileVM.SelectFile == null)
                return null;
            return uploadFileVM.SelectFile.FileInfo;
        }
        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            if (null == uploadFileVM || uploadFileVM.SelectFile == null)
                return;
            this.DialogResult = true;
            this.Close();
        }
    }
    class SelectUploadFileVM : NotifyPropertyChangedBase
    {
        private ObservableCollection<DBFileVM> allFiles { get; set; }
        public ObservableCollection<DBFileVM> AllFiles
        {
            get { return allFiles; }
            set
            {
                allFiles = value;
                this.RaisePropertyChanged();
            }
        }
        private DBFileVM selectFile { get; set; }
        public DBFileVM SelectFile 
        {
            get { return selectFile; }
            set 
            {
                selectFile = value;
                this.RaisePropertyChanged();
            }
        }
        public SelectUploadFileVM(List<DBProjectFileInfo> projectFileInfos) 
        {
            AllFiles = new ObservableCollection<DBFileVM>();
            foreach (var item in projectFileInfos) 
            {
                AllFiles.Add(new DBFileVM(item));
            }
        }
    }
    class DBFileVM : NotifyPropertyChangedBase
    {
        public DBProjectFileInfo FileInfo { get; set; }
        public DBFileVM(DBProjectFileInfo dBProjectFileInfo) 
        {
            FileInfo = dBProjectFileInfo;
        }
        public string Id 
        {
            get { return FileInfo.Id; }
            set 
            {
                FileInfo.Id = value;
                this.RaisePropertyChanged();
            }
        }
        public string Name 
        {
            get { return FileInfo.FileName; }
            set 
            {
                FileInfo.FileName = value;
                this.RaisePropertyChanged();
            }
        }
        public string MajorName
        {
            get { return FileInfo.Major; }
            set 
            {
                FileInfo.Major = value;
                this.RaisePropertyChanged();
            }
        }
        public string FileType 
        {
            get { return FileInfo.FileType; }
            set
            {
                FileInfo.FileType = value;
                this.RaisePropertyChanged();
            }
        }
    }
}
