using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCWindowModel : GFCElementModel
    {
        public double WindowHeight { get; set; }
        public double WindowLength { get; set; }

        public GFCWindowModel(ThGFC2Document gfcDoc, string matirial, int globalId, double windowLength, double height) : base(gfcDoc, matirial)
        {
            WindowHeight = height;
            WindowLength = windowLength;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcWindow(gfcDoc, -1, true, windowLength, height, 0, ref globalId);
        }

        public GFCWindowModel(ThGFC2Document gfcDoc, string matirial, DeductGFCModel window, int shapeId, bool isModel, int globalId, double windowLength, double height, double aboveFloorHeight) : base(gfcDoc, matirial)
        {
            WindowHeight = height;
            WindowLength = windowLength;
            IsConstruct = false;
            Model = window;
            ID = THModelToGFC2.ToGfcWindow(gfcDoc, shapeId, false, windowLength, height, aboveFloorHeight, ref globalId);
        }
    }
}
