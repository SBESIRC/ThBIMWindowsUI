using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Application;

namespace THBimEngine.Internal.ViewModel
{
    class SelectEntityVM : NotifyPropertyChangedBase
    {
        public ObservableCollection<FileFilterVM> _allFiles { get; set; }
        public ObservableCollection<FileFilterVM> AllFiles
        {
            get { return _allFiles; }
            set
            {
                _allFiles = value;
                this.RaisePropertyChanged();
            }
        }
        public SelectEntityVM()
        {
            AllFiles = new ObservableCollection<FileFilterVM>();
        }
    }
    public class FileFilterVM : NotifyPropertyChangedBase
    {
        public FileFilterVM(string projetId, string showName)
        {
            FileName = projetId;
            ShowName = showName;
            IsChecked = true;
        }
        private bool? _isChecked { get; set; }
        public bool? IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                _isChecked = value;
                this.RaisePropertyChanged();
            }
        }
        public string FileName { get; }
        public string ShowName { get; }
    }
}
