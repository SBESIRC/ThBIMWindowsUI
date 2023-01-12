using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Application;
using THBimEngine.Domain;

namespace XbimXplorer
{
    public class ExportViewModel : NotifyPropertyChangedBase
    {
        private ObservableCollection<THBimProjectViewModel> _allProjects { get; set; }
        public ObservableCollection<THBimProjectViewModel> AllProjects
        {
            get { return _allProjects; }
            set
            {
                _allProjects = value;
                this.RaisePropertyChanged();
            }
        }

        public ExportViewModel(THDocument document)
        {
            AllProjects = new ObservableCollection<THBimProjectViewModel>();
            foreach (var project in document.AllBimProjects)
            {
                AllProjects.Add(new THBimProjectViewModel(project));
            }
        }
    }

    public class THBimProjectViewModel : NotifyPropertyChangedBase
    {

        public THBimProjectViewModel(THBimProject project)
        {
            Project = project;
            ShowName = project.Name;
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
        public THBimProject Project { get; }
        public string ShowName { get; }
    }
}
