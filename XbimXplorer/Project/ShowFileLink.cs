using System;
using THBimEngine.Application;
using Xbim.Common.Geometry;

namespace XbimXplorer
{
    public class ShowFileLink : NotifyPropertyChangedBase
    {
        public string LinkId { get; set; }
        public string FromLinkId { get; set; }
        public string ProjectFileId { get; set; }
        public string LinkProjectFileId { get; set; }
        public ShowProjectFile LinkProject { get; set; }
        private int state { get; set; }
        /// <summary>
        /// 链接状态，0，已链接，1已卸载
        /// </summary>
        public int State 
        {
            get { return state; }
            set 
            {
                state = value;
                this.RaisePropertyChanged();
                RaisePropertyChanged("LinkState");
            }
        }
        public string LinkState
        {
            get { return State < 1 ? "已链接" : "已卸载"; }
        }
        public double MoveX { get; set; }
        public double MoveY { get; set; }
        public double MoveZ { get; set; }
        public double RotainAngle { get; set; }
        public XbimMatrix3D GetLinkMatrix3D
        {
            get
            {
                var tempVector = new XbimVector3D(MoveX - 0.0, MoveY - 0.0, MoveZ - 0.0);
                XbimMatrix3D matrix3D = XbimMatrix3D.CreateTranslation(tempVector);
                matrix3D.RotateAroundZAxis(Math.PI * RotainAngle / 180.0);
                return matrix3D;
            }
        }
    }
}
