using System.Collections.ObjectModel;
using THBimEngine.Application;
using Xbim.Common.Geometry;

namespace XbimXplorer.LeftTabItme
{
    class LinkViewModel : NotifyPropertyChangedBase
    {
        LinkModel _selectModel { get; set; }
        public LinkModel SelectModel
        {
            get { return _selectModel; }
            set
            {
                _selectModel = value;
                this.RaisePropertyChanged();
            }
        }
        ObservableCollection<LinkModel> _allLinkModel { get; set; }
        public ObservableCollection<LinkModel> AllLinkModel
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
            AllLinkModel = new ObservableCollection<LinkModel>();
        }
    }
    public class LinkModel
    {
        public ProjectModel Project { get; set; }
        public string LinkState { get; set; }
        public XbimMatrix3D MoveMatrix3D { get; set; }
        public double RotainAngle { get; set; }
    }
}
