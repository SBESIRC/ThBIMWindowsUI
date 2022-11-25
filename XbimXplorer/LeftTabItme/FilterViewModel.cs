using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using THBimEngine.Application;
using THBimEngine.Domain;

namespace XbimXplorer.LeftTabItme
{
    class FilterViewModel: NotifyPropertyChangedBase
    {
        IEngineApplication engineApp;
        THDocument document;
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
        private Dictionary<string, List<FilterBase>> PrjAllFilters { get; }
        private HashSet<int> ShowEntityGIndex { get; set; }
        private int AllEntityCount = 0;
        private object inUpdata = false;

        public FilterViewModel(IEngineApplication engineApplication) 
        {
            engineApp = engineApplication;
            AllFloorFilters = new ObservableCollection<FloorFilterViewModel>();
            AllTypeFilters = new ObservableCollection<EntityTypeFilterViewModel>();
            AllFileFilters = new ObservableCollection<FileFilterViewModel>();
            PrjAllFilters = new Dictionary<string, List<FilterBase>>();
            ShowEntityGIndex = new HashSet<int>();
        }
        public void UpdataFilterByCurrentDocument(THDocument document) 
        {
            AllFloorFilters.Clear();
            AllTypeFilters.Clear();
            AllFileFilters.Clear();
            PrjAllFilters.Clear();
            ShowEntityGIndex.Clear();
            if (null == document)
                return;
            UpdataProjectFilter(document);


            CalcTypeFilter();
            CalcFloorFilter();
            CalcFileFilter();
        }
        private void CalcFloorFilter() 
        {
            var allFloors = ProjectExtension.AllProjectStoreys(document.AllBimProjects);
            AllFloorFilters.Clear();
            FloorShowIds.Clear();
            foreach (var floorKeyValue in allFloors)
            {
                var prjStoreyFilters = new Dictionary<string, StoreyFilter>();
                foreach (var filters in PrjAllFilters)
                {
                    var floorFilters = filters.Value.OfType<StoreyFilter>().ToList();
                    if (floorFilters.Count < 1)
                        continue;
                    foreach (var filter in floorFilters) 
                    {
                        if (filter.Describe != floorKeyValue.Name)
                            continue;
                        if (prjStoreyFilters.ContainsKey(filters.Key))
                            continue;
                        prjStoreyFilters.Add(filters.Key, filter);
                    }
                }
                var showElevation = (floorKeyValue.Elevation / 1000.0).ToString("N3");
                var addFilter = new FloorFilterViewModel(floorKeyValue.Name, showElevation, floorKeyValue.Height < 0 ? "-" : floorKeyValue.Height.ToString());
                foreach (var item in prjStoreyFilters) 
                {
                    addFilter.ProjectFilters.Add(item.Key, item.Value);
                }
                AllFloorFilters.Add(addFilter);
            }
            foreach (var filter in AllFloorFilters)
            {
                var tempIds = GetGlobalIndexByFilter(filter.ProjectFilters);
                FloorShowIds = TypeShowIds.Union(tempIds).ToHashSet();
            }
        }
        private void CalcTypeFilter() 
        {
            AllTypeFilters.Clear();
            TypeShowIds.Clear();
            foreach (var item in PrjAllFilters) 
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
            foreach (var filter in AllTypeFilters)
            {
                var tempIds = GetGlobalIndexByFilter(filter.ProjectFilters);
                TypeShowIds = TypeShowIds.Union(tempIds).ToHashSet();
            }
        }
        private void CalcFileFilter() 
        {
            AllFileFilters.Clear();
            ProjectShowIds.Clear();
            foreach (var item in PrjAllFilters)
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
            foreach (var prjFilter in AllFileFilters)
            {
                var tempIds = GetGlobalIndexByFilter(prjFilter.ProjectFilters);
                ProjectShowIds = ProjectShowIds.Union(tempIds).ToHashSet();
            }
        }

        private HashSet<int> ProjectShowIds = new HashSet<int>();
        private HashSet<int> TypeShowIds = new HashSet<int>();
        private HashSet<int> FloorShowIds = new HashSet<int>();
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
            var tempIds = GetGlobalIndexByFilter(typeFilter.ProjectFilters);
            if (typeFilter.IsChecked == true)
            {
                //新增显示
                ProjectShowIds = ProjectShowIds.Union(tempIds).ToHashSet();
            }
            else 
            {
                //移除显示
                ProjectShowIds = ProjectShowIds.Except(tempIds).ToHashSet();
            }
            ShowFilterResult();
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
            var tempIds = GetGlobalIndexByFilter(typeFilter.ProjectFilters);
            if (typeFilter.IsChecked == true)
            {
                //新增显示
                ProjectShowIds = ProjectShowIds.Union(tempIds).ToHashSet();
            }
            else
            {
                //移除显示
                ProjectShowIds = ProjectShowIds.Except(tempIds).ToHashSet();
            }
            ShowFilterResult();
        }
        #endregion

        #region 楼层过滤事件
        public void UpdataFloorShowIds() 
        {
            FloorShowIds.Clear();
            foreach (var filter in AllFloorFilters)
            {
                if (filter.IsChecked != true)
                    continue;
                var tempIds = GetGlobalIndexByFilter(filter.ProjectFilters);
                FloorShowIds = FloorShowIds.Union(tempIds).ToHashSet();
            }
        }
        #endregion
        public void ShowFilterResult()
        {
            var showIds = GetAllShowIdByFilters();
            var entityIds = new List<int>();
            var gridIds = new List<string>();
            var entityCount = engineApp.CurrentDocument.AllGeoModels.Count;
            foreach (var item in showIds) 
            {
                if (item <= entityCount)
                {
                    entityIds.Add(item);
                }
                else 
                {
                    gridIds.Add(engineApp.CurrentDocument.MeshEntiyRelationIndexs[item].ProjectEntityId);
                }
            }
            engineApp.ShowEntityByIds(entityIds);
            engineApp.ShowGridByIds(gridIds);

        }
        private HashSet<int> GetAllShowIdByFilters()
        {
            HashSet<int> showIds = new HashSet<int>();
            //step1 项目获取所有Id
            showIds = ProjectShowIds.Intersect(TypeShowIds).ToHashSet();
            showIds = showIds.Intersect(FloorShowIds).ToHashSet();
            return showIds;
        }
        #endregion
        public void UpdataProjectFilter(THDocument document)
        {
            this.document = document;
            lock (inUpdata)
            {
                inUpdata = true;
                PrjAllFilters.Clear();
                ShowEntityGIndex.Clear();
                ShowEntityGIndex = document.MeshEntiyRelationIndexs.Keys.ToHashSet();
                AllEntityCount = ShowEntityGIndex.Count;
                var storeyFilters = ProjectExtension.GetProjectStoreyFilters(document.AllBimProjects); // 获取所有的 storey filter
                var typeFilters = ProjectExtension.GetProjectTypeFilters(document.AllBimProjects); // 获取所有的 type filter
                foreach (var project in document.AllBimProjects)
                {
                    var filter = new ProjectFilter(new List<string> { project.ProjectIdentity });
                    filter.Describe = project.Name;
                    var listFilters = new List<FilterBase>();
                    listFilters.Add(filter);
                    foreach (var storeyFilter in storeyFilters)
                    {
                        var copyItem = storeyFilter.Clone() as StoreyFilter;
                        listFilters.Add(copyItem);
                    }
                    foreach (var typeFilter in typeFilters)
                    {
                        var copyItem = typeFilter.Clone() as TypeFilter;
                        listFilters.Add(copyItem);
                    }
                    ProjectExtension.PorjectFilterEntitys(project, listFilters); // 获取所有的数据
                    PrjAllFilters.Add(project.ProjectIdentity, listFilters);
                }
                inUpdata = false;
            }
        }
        private HashSet<int> GetGlobalIndexByFilterIds(Dictionary<string, HashSet<string>> prjFilterIds)
        {
            var resIds = new HashSet<int>();
            Parallel.ForEach(document.MeshEntiyRelationIndexs.Values, new ParallelOptions() { }, item =>
            {
                var prjId = item.ProjectId;
                if (!prjFilterIds.ContainsKey(prjId))
                    return;
                var prjEntityId = item.ProjectEntityId;
                var filterIds = prjFilterIds[prjId];
                if (filterIds.Contains(prjEntityId))
                {
                    lock (resIds)
                    {
                        if (!resIds.Contains(item.GlobalMeshIndex))
                            resIds.Add(item.GlobalMeshIndex);
                    }
                }
            });
            return resIds;
        }
        private Dictionary<string, HashSet<string>> GetProjectFilterStrIds(Dictionary<string, List<FilterBase>> prjFilters)
        {
            var res = new Dictionary<string, HashSet<string>>();
            foreach (var item in prjFilters)
            {
                var filters = item.Value;
                var filterIds = new HashSet<string>();
                foreach (var filter in filters)
                {
                    foreach (var id in filter.ResultElementUids)
                    {
                        if (filterIds.Contains(id))
                            continue;
                        filterIds.Add(id);
                    }
                }
                res.Add(item.Key, filterIds);
            }
            return res;
        }
        private Dictionary<string, HashSet<string>> GetProjectFilterStrIds(Dictionary<string, FilterBase> prjFilters)
        {
            var res = new Dictionary<string, HashSet<string>>();
            foreach (var item in prjFilters)
            {
                var filter = item.Value;
                var filterIds = new HashSet<string>();
                foreach (var id in filter.ResultElementUids)
                {
                    if (filterIds.Contains(id))
                        continue;
                    filterIds.Add(id);
                }
                res.Add(item.Key, filterIds);
            }
            return res;
        }
        public HashSet<int> GetGlobalIndexByFilter(Dictionary<string, List<FilterBase>> prjFilters)
        {
            var filterIds = GetProjectFilterStrIds(prjFilters);
            return GetGlobalIndexByFilterIds(filterIds);
        }
        public HashSet<int> GetGlobalIndexByFilter(Dictionary<string, FilterBase> prjFilters)
        {
            var filterIds = GetProjectFilterStrIds(prjFilters);
            return GetGlobalIndexByFilterIds(filterIds);
        }
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
