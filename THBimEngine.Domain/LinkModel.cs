using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public class LinkModel
    {
        public string LinkId { get; set; }
        public ProjectFileInfo Project { get; set; }
        public string LinkState { get; set; }
        public XbimMatrix3D MoveMatrix3D { get; set; }
        public double RotainAngle { get; set; }
    }
}
