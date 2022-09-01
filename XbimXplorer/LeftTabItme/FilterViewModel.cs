using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using THBimEngine.Domain;
using XbimXplorer.ThBIMEngine;

namespace XbimXplorer.LeftTabItme
{
    class FilterViewModel: NotifyPropertyChangedBase
    {
        private ThBimFilterController bimFilterController;
        public static readonly FilterViewModel Instance =new FilterViewModel();
        private ObservableCollection<FloorFilterViewModel> _allFloorFilters { get; set; }
        public ObservableCollection<FloorFilterViewModel> AllFloorFilters 
        {
            get { return _allFloorFilters; }
            set
            {
                _allFloorFilters = value;
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<EntityTypeFilterViewModel> _allTypeFilters { get; set; }
        public ObservableCollection<EntityTypeFilterViewModel> AllTypeFilters
        {
            get { return _allTypeFilters; }
            set
            {
                _allTypeFilters = value;
                this.RaisePropertyChanged();
            }
        }
        public ObservableCollection<FileFilterViewModel> _allFileFilters { get; set; }
        public ObservableCollection<FileFilterViewModel> AllFileFilters
        {
            get { return _allFileFilters; }
            set
            {
                _allFileFilters = value;
                this.RaisePropertyChanged();
            }
        }
        FilterViewModel() 
        {
            AllFloorFilters = new ObservableCollection<FloorFilterViewModel>();
            AllTypeFilters = new ObservableCollection<EntityTypeFilterViewModel>();
            AllFileFilters = new ObservableCollection<FileFilterViewModel>();
            bimFilterController = new ThBimFilterController();
        }
        public void UpdataFilterByProject() 
        {
            bimFilterController.UpdataProjectFilter();

            //更新 //AllFloorFilters //AllTypeFilters //AllFileFilters


            //bimFilterController.PrjAllFilters
            // 1、
            // 文件载入时更新 显示的全选内容 & PrjAllFilters
            //Dictionary<string, List<FilterBase>> PrjAllFilters; 
            //AllFloorFilters
            //AllTypeFilters
            //AllFileFilters

            CalcTypeFilter();
            CalcFloorFilter();
            CalcFileFilter();
        }
        private void CalcFloorFilter() 
        {
            var allFloors = ProjectExtension.AllProjectStoreys(THBimScene.Instance.AllBimProjects);
            AllFloorFilters.Clear();
            foreach (var floorKeyValue in allFloors)
            {
                var prjStoreyFilters = new Dictionary<string, StoreyFilter>();
                foreach (var filters in bimFilterController.PrjAllFilters)
                {
                    var floorFilters = filters.Value.OfType<StoreyFilter>().ToList();
                    if (floorFilters.Count < 1)
                        continue;
                    foreach (var filter in floorFilters) 
                    {
                        if (filter.Describe != floorKeyValue.Key)
                            continue;
                        if (prjStoreyFilters.ContainsKey(filters.Key))
                            continue;
                        prjStoreyFilters.Add(filters.Key, filter);
                    }
                }
                var addFilter = new FloorFilterViewModel(floorKeyValue.Key, floorKeyValue.Value.First().Elevation.ToString(), floorKeyValue.Value.First().LevelHeight.ToString());
                foreach (var item in prjStoreyFilters) 
                {
                    addFilter.ProjectFilters.Add(item.Key, item.Value);
                }
                AllFloorFilters.Add(addFilter);
            }
        }
        private void CalcTypeFilter() 
        {
            AllTypeFilters.Clear();
            foreach (var item in bimFilterController.PrjAllFilters) 
            {
                var typeFilters = item.Value.OfType<TypeFilter>().ToList();
                if (typeFilters.Count < 1)
                    continue;
                foreach (var type in typeFilters) 
                {
                    var strTypeName = type.Describe;
                    var hisFile = AllTypeFilters.Where(c => c.TypeName == strTypeName).FirstOrDefault();
                    if (hisFile == null) 
                    {
                        hisFile = new EntityTypeFilterViewModel(strTypeName);
                        AllTypeFilters.Add(hisFile);
                    }
                    if (hisFile.ProjectFilters.ContainsKey(item.Key))
                    {
                        continue;
                    }
                    else 
                    {
                        hisFile.ProjectFilters.Add(item.Key, type);
                    }
                }
            }
        }
        private void CalcFileFilter() 
        {
            AllFileFilters.Clear();
            foreach (var item in bimFilterController.PrjAllFilters)
            {
                var prjFilters = item.Value.OfType<ProjectFilter>().ToList();
                if (prjFilters.Count < 1)
                    continue;
                foreach (var type in prjFilters)
                {
                    var strName = type.Describe;
                    var hisFile = AllFileFilters.Where(c => c.ShowName == strName).FirstOrDefault();
                    if (hisFile == null)
                    {
                        hisFile = new FileFilterViewModel(strName,strName);
                        AllFileFilters.Add(hisFile);
                    }
                    if (hisFile.ProjectFilters.ContainsKey(item.Key))
                    {
                        continue;
                    }
                    else
                    {
                        hisFile.ProjectFilters.Add(item.Key, type);
                    }
                }
            }
        }
        #region 事件的相应处理

        #region 类别过滤的事件
        RelayCommand<CheckBox> typeFilterCheckedChange;
        public ICommand TypeCheckedCommond
        {
            get
            {
                if (typeFilterCheckedChange == null)
                    typeFilterCheckedChange = new RelayCommand<CheckBox>((checkBox) => UpdataTypeState(checkBox));

                return typeFilterCheckedChange;
            }
        }
        private void UpdataTypeState(CheckBox typeCheckBox)
        {
            var typeFilter = typeCheckBox.DataContext as EntityTypeFilterViewModel;
            var tempIds = bimFilterController.GetGlobalIndexByFilter(typeFilter.ProjectFilters);
            if (typeFilter.IsChecked == true)
            {
                //新增显示
                UpdataSelectAllState(new HashSet<int>(), tempIds);
            }
            else 
            {
                //移除显示
                UpdataSelectAllState(tempIds,new HashSet<int>());
            }
        }
        #endregion

        #region 文件过滤的事件
        RelayCommand<CheckBox> fileFilterCheckedChange;
        public ICommand FileCheckedCommond
        {
            get
            {
                if (fileFilterCheckedChange == null)
                    fileFilterCheckedChange = new RelayCommand<CheckBox>((checkBox) => UpdataFileState(checkBox));

                return fileFilterCheckedChange;
            }
        }
        private void UpdataFileState(CheckBox fileCheckBox) 
        {
            var typeFilter = fileCheckBox.DataContext as FileFilterViewModel;
            var tempIds = bimFilterController.GetGlobalIndexByFilter(typeFilter.ProjectFilters);
            if (typeFilter.IsChecked == true)
            {
                //新增显示
                UpdataSelectAllState(new HashSet<int>(), tempIds);
            }
            else
            {
                //移除显示
                UpdataSelectAllState(tempIds, new HashSet<int>());
            }
        }
        #endregion

        #region 楼层过滤事件
        #endregion
        private void UpdataSelectAllState(HashSet<int> delIds,HashSet<int> addIds)
        {
            bimFilterController.ShowEntityByFilter(delIds, addIds);

        }
        #endregion
    }
    public class FloorFilterViewModel : NotifyPropertyChangedBase
    {
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
        public FloorFilterViewModel(string floorName,string elevation,string levelHeight) 
        {
            ProjectFilters = new Dictionary<string, FilterBase>();
            FloorName = floorName;
            Elevation = elevation;
            LevelHeight = levelHeight;
            IsChecked = true;
        }
        public string FloorName { get; }
        public string Elevation { get;  }
        public string LevelHeight { get;}
        public Dictionary<string,FilterBase> ProjectFilters { get;  }
    }
    public class EntityTypeFilterViewModel : NotifyPropertyChangedBase
    {
        public EntityTypeFilterViewModel(string typeName) 
        {
            ProjectFilters = new Dictionary<string, FilterBase>();
            TypeName = typeName;
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
        public string TypeName { get; }
        public Dictionary<string, FilterBase> ProjectFilters { get; }
    }
    public class FileFilterViewModel : NotifyPropertyChangedBase
    {
        public FileFilterViewModel(string fileName,string showName) 
        {
            ProjectFilters = new Dictionary<string, FilterBase>();
            FileName = fileName;
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
        public string FileName { get;}
        public string ShowName { get;}
        public Dictionary<string, FilterBase> ProjectFilters { get; }
    }
}
