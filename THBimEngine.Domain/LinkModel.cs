using Xbim.Common.Geometry;

namespace THBimEngine.Domain
{
    public class LinkModel
    {
        public string LinkId { get; set; }
        public ShortProjectFile Project { get; set; }
        public string LinkState { get; set; }
        public double MoveX { get; set; }
        public double MoveY { get; set; }
        public double MoveZ { get; set; }
        public double RotainAngle { get; set; }
    }
}
