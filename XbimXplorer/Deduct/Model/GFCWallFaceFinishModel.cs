using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCWallFaceFinishModel : GFCElementModel
    {
        public int WallFaceFinishThickness { get; set; }
        public GFCWallFaceFinishModel(ThGFC2Document gfcDoc, string matirial, int globalId,string wallFaceFinishName, int thickness) : base(gfcDoc, matirial)
        {
            WallFaceFinishThickness = thickness;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcWallFaceFinish(gfcDoc, ref globalId, wallFaceFinishName, thickness, -1, "墙底标高", "墙顶标高", 50);
        }

        public GFCWallFaceFinishModel(ThGFC2Document gfcDoc, string matirial, int shapeId, int globalId, string wallFaceFinishName, int thickness, string btmElevS, string topElevS, int axisOffset) : base(gfcDoc, matirial)
        {
            WallFaceFinishThickness = thickness;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcWallFaceFinish(gfcDoc, ref globalId, wallFaceFinishName, thickness, shapeId, btmElevS, topElevS, axisOffset);
        }
    }
}
