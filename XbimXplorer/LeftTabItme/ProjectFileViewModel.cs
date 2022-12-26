using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using THBimEngine.Application;
using THBimEngine.Domain;

namespace XbimXplorer.LeftTabItme
{
    class ProjectFileViewModel : NotifyPropertyChangedBase
    {
        private List<ShowProjectFile> allProjectAllFileModels;
        ObservableCollection<ShowProjectFile> _projectAllFileModels { get; set; }
        
        public ObservableCollection<ShowProjectFile> ProjectAllFileModels
        {
            get { return _projectAllFileModels; }
            set
            {
                _projectAllFileModels = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<string> _buildingNames { get; set; }
        public ObservableCollection<string> BuildingNames
        {
            get { return _buildingNames; }
            set
            {
                _buildingNames = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<string> _catagoryNames { get; set; }
        public ObservableCollection<string> CatagoryNames
        {
            get { return _catagoryNames; }
            set
            {
                _catagoryNames = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<string> _systemNames { get; set; }
        public ObservableCollection<string> SystemNames
        {
            get { return _systemNames; }
            set
            {
                _systemNames = value;
                this.RaisePropertyChanged();
            }
        }
        public ProjectFileViewModel(List<ShowProjectFile> showProjectFiles)
        {
            ProjectAllFileModels = new ObservableCollection<ShowProjectFile>();
            BuildingNames = new ObservableCollection<string>();
            CatagoryNames = new ObservableCollection<string>();
            SystemNames = new ObservableCollection<string>();
            allProjectAllFileModels = showProjectFiles;
            var allBuildingNames = showProjectFiles.Select(c => c.SubPrjName).Distinct().ToList();
            var allTypes = showProjectFiles.Select(c => c.ShowSourceName).Distinct().ToList();
            var allCatagory = showProjectFiles.Select(c => c.MajorName).Distinct().ToList();
            BuildingNames.Add("全部");
            foreach (var item in allBuildingNames)
            {
                BuildingNames.Add(item);
            }
            SelectBuilding = BuildingNames.First();
            CatagoryNames.Add("全部");
            foreach (var item in allCatagory)
            {
                CatagoryNames.Add(item);
            }
            SelectCatagory = CatagoryNames.First();
            SystemNames.Add("全部");
            foreach (var item in allTypes)
            {
                SystemNames.Add(item);
            }
            SelectSystem = SystemNames.First();
        }
        string selectSystem { get; set; }
        public string SelectSystem
        {
            get { return selectSystem; }
            set 
            {
                selectSystem = value;
                this.RaisePropertyChanged();
                ShowProjectModelByFilter();
            }
        }
        string selectCatagory { get; set; }
        public string SelectCatagory
        {
            get { return selectCatagory; }
            set
            {
                selectCatagory = value;
                this.RaisePropertyChanged();
                ShowProjectModelByFilter();
            }
        }
        string selectBuilding { get; set; }
        public string SelectBuilding
        {
            get { return selectBuilding; }
            set
            {
                selectBuilding = value;
                this.RaisePropertyChanged();
                ShowProjectModelByFilter();
            }
        }
        private void ShowProjectModelByFilter() 
        {
            var filterBuilding = (string.IsNullOrEmpty(selectBuilding) || selectBuilding == "全部") ? string.Empty : selectBuilding;
            var filterSystem = (string.IsNullOrEmpty(selectSystem) || selectSystem == "全部") ? string.Empty : selectSystem;
            var filterCatagory = (string.IsNullOrEmpty(selectCatagory) || selectCatagory == "全部") ? string.Empty : selectCatagory;
            ProjectAllFileModels.Clear();
            foreach (var item in allProjectAllFileModels) 
            {
                if (!string.IsNullOrEmpty(filterBuilding) && filterBuilding != item.SubPrjName)
                    continue;
                if (!string.IsNullOrEmpty(filterSystem) && filterSystem != item.ShowSourceName)
                    continue;
                if (!string.IsNullOrEmpty(filterCatagory) && filterCatagory != item.MajorName)
                    continue;
                ProjectAllFileModels.Add(item);
            }
        }
    }
}
