using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            Dictionary<string, List<FilterBase>> PrjAllFilters; 
            //AllFloorFilters
            //AllTypeFilters
            //AllFileFilters

            // 2、
            // 全选、反选、多选选中：
            // 将选中内容通过PrjAllFilters为base来过滤入prjFilterIds中
            Dictionary<string, HashSet<string>> prjFilterIds = new Dictionary<string, HashSet<string>>();
            
        }

        RelayCommand listCheckedChange;
        public ICommand CheckedCommond
        {
            get
            {
                if (listCheckedChange == null)
                    listCheckedChange = new RelayCommand(() => UpdateSelectAllState());

                return listCheckedChange;
            }
        }
        private void UpdateSelectAllState()
        {


        }
    }
    public class FloorFilterViewModel : NotifyPropertyChangedBase
    {
        public string FloorName { get; set; }
        public string Elevation { get; set; }
        public string LevelHeight { get; set; }
        public Dictionary<string,FilterBase> ProjectFilters { get; set; }
    }
    public class EntityTypeFilterViewModel : NotifyPropertyChangedBase
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
        private string _typeName { get; set; }
        public string TypeName
        {
            get
            {
                return _typeName;
            }
            set
            {
                _typeName = value;
                this.RaisePropertyChanged();
            }
        }
        public Dictionary<string, FilterBase> ProjectFilters { get; set; }
    }
    public class FileFilterViewModel : NotifyPropertyChangedBase
    {
        public string FileName { get; set; }
        public string ShowName { get; set; }
        public Dictionary<string, FilterBase> ProjectFilters { get; set; }
    }
}
