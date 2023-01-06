using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCFaceFloorFinishModel : GFCElementModel
    {
        public int FaceFloorFinishThickness { get; set; }
        public GFCFaceFloorFinishModel(ThGFC2Document gfcDoc, string matirial, int globalId, int thickness) : base(gfcDoc, matirial)
        {
            FaceFloorFinishThickness = thickness;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcFaceFloorFinish(gfcDoc, ref globalId, thickness, "层底标高", -1);
        }

        public GFCFaceFloorFinishModel(ThGFC2Document gfcDoc, string matirial, int shapeId, int globalId, int thickness,string topElev) : base(gfcDoc, matirial)
        {
            FaceFloorFinishThickness = thickness;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcFaceFloorFinish(gfcDoc, ref globalId, thickness, topElev, shapeId);
        }
    }
}
