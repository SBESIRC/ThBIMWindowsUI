using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using THBimEngine.Application;

namespace XbimXplorer.Project
{
    class FileHistoryVM : NotifyPropertyChangedBase
    {
        public List<string> ShowFilterFileNames { get; set; }
        public List<FileHistory> allFileHis;
        ObservableCollection<FileHistoryDetailVM> _showFileModels { get; set; }
        public ObservableCollection<FileHistoryDetailVM> ShowFiles
        {
            get { return _showFileModels; }
            set
            {
                _showFileModels = value;
                this.RaisePropertyChanged();
            }
        }
        public FileHistoryVM(List<FileHistory> fileHistories) 
        {
            ShowFiles = new ObservableCollection<FileHistoryDetailVM>();
            allFileHis = new List<FileHistory>();
            ShowFilterFileNames = new List<string>();
            foreach (var item in fileHistories) 
            {
                ShowFilterFileNames.Add(item.MainFileName);
                allFileHis.Add(item);
            }
        }
        public void ProjectFileChangeUseVersion(string prjFileId, string useFileUplaodId) 
        {
            //1、改基础原数据
            foreach (var item in allFileHis)
            {
                if (item.MainFileId != prjFileId)
                    continue;
                item.NewMainFileUplaodId = useFileUplaodId;
                foreach (var file in item.FileHistoryDetails)
                {
                    file.IsCurrentVersion = file.ProjectFileUplaodId == useFileUplaodId;
                }
            }
            //2、刷新显示数据
            foreach (var item in ShowFiles) 
            {
                if (item.ProjectFileId != prjFileId)
                    continue;
                item.IsCurrentVersion = item.ProjectFileUplaodId == useFileUplaodId;
            }
        }
        public void ProjectFileActive(string prjFileId, string useFileUplaodId) 
        {
            //1、改基础原数据
            foreach (var item in allFileHis)
            {
                if (item.MainFileId != prjFileId)
                    continue;
                item.NewMainFileUplaodId = useFileUplaodId;
                item.NewState = "";
                foreach (var file in item.FileHistoryDetails)
                {
                    file.NewState = "";
                    file.IsCurrentVersion = file.ProjectFileUplaodId == useFileUplaodId;
                }
            }
            //2、刷新显示数据
            foreach (var item in ShowFiles)
            {
                if (item.ProjectFileId != prjFileId)
                    continue;
                item.NewState = "";
                item.IsCurrentVersion = item.ProjectFileUplaodId == useFileUplaodId;
            }
        }
        public void FilterByFileName(string filterName) 
        {
            ShowFiles.Clear();
            var temp = new List<FileHistoryDetailVM>();
            if (string.IsNullOrEmpty(filterName))
            {
                //显示全部
                foreach (var item in allFileHis) 
                {
                    foreach (var file in item.FileHistoryDetails)
                        temp.Add(new FileHistoryDetailVM(file));
                }
            }
            else 
            {
                //根据名称过滤
                foreach (var item in allFileHis)
                {
                    if (item.MainFileName != filterName)
                        continue;
                    foreach (var file in item.FileHistoryDetails)
                        temp.Add(new FileHistoryDetailVM(file));
                }
            }
            temp = temp.OrderByDescending(c => c.FileUploadTime).ToList();
            foreach (var item in temp)
                ShowFiles.Add(item);
        }
    }

    public class FileHistoryDetailVM : NotifyPropertyChangedBase
    {
        public FileHistoryDetail fileHistoryDetail { get; }
        public string ProjectFileId
        {
            get { return fileHistoryDetail.ProjectFileId; }
            set
            {
                fileHistoryDetail.ProjectFileId = value;
                this.RaisePropertyChanged();
            }
        }
        public string ProjectFileUplaodId 
        {
            get { return fileHistoryDetail.ProjectFileUplaodId; }
            set 
            {
                fileHistoryDetail.ProjectFileUplaodId =value;
                this.RaisePropertyChanged();
            }
        }
        public string ProjectFileUploadVersionId
        {
            get { return fileHistoryDetail.ProjectFileUploadVersionId; }
            set
            {
                fileHistoryDetail.ProjectFileUploadVersionId = value;
                this.RaisePropertyChanged();
            }
        }
        public DateTime FileUploadTime
        {
            get { return fileHistoryDetail.FileUploadTime; }
            set
            {
                fileHistoryDetail.FileUploadTime = value;
                this.RaisePropertyChanged();
            }
        }
        public string UploaderName
        {
            get { return fileHistoryDetail.UploaderName; }
            set
            {
                fileHistoryDetail.UploaderName = value;
                this.RaisePropertyChanged();
            }
        }
        public string ShowFileName
        {
            get { return fileHistoryDetail.ShowFileName; }
            set
            {
                fileHistoryDetail.ShowFileName = value;
                this.RaisePropertyChanged();
            }
        }
        public bool IsCurrentVersion
        {
            get { return fileHistoryDetail.IsCurrentVersion; }
            set
            {
                fileHistoryDetail.IsCurrentVersion = value;
                SetRowColor();
                this.RaisePropertyChanged();
            }
        }
        public bool CanChange
        {
            get { return fileHistoryDetail.CanChange; }
            set
            {
                fileHistoryDetail.CanChange = value;
                this.RaisePropertyChanged();
            }
        }
        public string State
        {
            get { return fileHistoryDetail.State; }
            set
            {
                fileHistoryDetail.State = value;
                this.RaisePropertyChanged();
            }
        }
        public string NewState
        {
            get { return fileHistoryDetail.NewState; }
            set
            {
                fileHistoryDetail.NewState = value;
                SetRowColor();
                this.RaisePropertyChanged();
            }
        }
        private Brush rowColor { get; set; }
        public Brush RowColor 
        {
            get { return rowColor; }
            set 
            {
                rowColor = value;
                this.RaisePropertyChanged();
            }
        }
        private Brush rowFrontColor { get; set; }
        public Brush RowFrontColor
        {
            get { return rowFrontColor; }
            set
            {
                rowFrontColor = value;
                this.RaisePropertyChanged();
            }
        }
        public FileHistoryDetailVM(FileHistoryDetail fileHistory)
        {
            fileHistoryDetail = fileHistory;
            SetRowColor();
        }
        void SetRowColor()
        {
            RowColor = Brushes.Transparent;
            if (NewState == "已作废") 
            {
                RowFrontColor = Brushes.Gray;
            }
            else if (IsCurrentVersion) 
            {
                RowFrontColor = Brushes.Red;
            }
            else
            {
                RowFrontColor = Brushes.Black;
            }
        }
    }
}
