using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCWallModel : GFCElementModel
    {
        public int WallThickness { get; set; }

        public GFCWallModel(ThGFC2Document gfcDoc, string matirial, int globalId, int leftWidth, int width) : base(gfcDoc, matirial)
        {
            WallThickness = width;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcArchiWall(gfcDoc, matirial, -1, leftWidth, width, "层底标高", "层顶标高", ref globalId);
        }

        public GFCWallModel(ThGFC2Document gfcDoc, string matirial, DeductGFCModel wall, int globalId, int shapeId, int leftWidth, int width, string btmElevS, string topElevS) : base(gfcDoc, matirial)
        {
            WallThickness = width;
            IsConstruct = false;
            Model = wall;
            ID = THModelToGFC2.ToGfcArchiWall(gfcDoc, matirial, shapeId, leftWidth, width, btmElevS, topElevS, ref globalId);
        }
    }
}
