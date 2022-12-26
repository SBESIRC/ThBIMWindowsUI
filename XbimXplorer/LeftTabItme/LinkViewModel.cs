using System.Collections.ObjectModel;
using THBimEngine.Application;
using THBimEngine.Domain;
using Xbim.Common.Geometry;

namespace XbimXplorer.LeftTabItme
{
    class LinkViewModel : NotifyPropertyChangedBase
    {
        ShowFileLink _selectModel { get; set; }
        public ShowFileLink SelectModel
        {
            get { return _selectModel; }
            set
            {
                _selectModel = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<ShowFileLink> _allLinkModel { get; set; }
        public ObservableCollection<ShowFileLink> AllLinkModel
        {
            get { return _allLinkModel; }
            set
            {
                _allLinkModel = value;
                this.RaisePropertyChanged();
            }
        }
        public LinkViewModel()
        {
            AllLinkModel = new ObservableCollection<ShowFileLink>();
        }
        public void ClearData() 
        {
            AllLinkModel.Clear();
            SelectModel = null;
        }
    }
    
}
