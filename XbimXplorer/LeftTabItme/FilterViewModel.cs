using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Domain;

namespace XbimXplorer.LeftTabItme
{
    class FilterViewModel
    {
        public ObservableCollection<FloorFilterViewModel> AllFloorFilters { get; }
        public ObservableCollection<EntityTypeFilterViewModel> AllTypeFilters { get; }
        public ObservableCollection<FileFilterViewModel> AllFileFilters { get; }
        public FilterViewModel() 
        {
            AllFloorFilters = new ObservableCollection<FloorFilterViewModel>();
            AllTypeFilters = new ObservableCollection<EntityTypeFilterViewModel>();
            AllFileFilters = new ObservableCollection<FileFilterViewModel>();
        }
        public void UpdataFilterByProject() 
        {
            //AllFloorFilters
        }
    }
    public class FloorFilterViewModel 
    {
        public string FloorName { get; set; }
        public string Elevation { get; set; }
        public string LevelHeight { get; set; }
        public Dictionary<string,FilterBase> ProjectFilters { get; set; }
    }
    public class EntityTypeFilterViewModel 
    {
        public string TypeName { get; }
        public Dictionary<string, FilterBase> ProjectFilters { get; set; }
    }
    public class FileFilterViewModel 
    {
        public string FileName { get; set; }
        public string ShowName { get; set; }
        public Dictionary<string, FilterBase> ProjectFilters { get; set; }
    }
}
