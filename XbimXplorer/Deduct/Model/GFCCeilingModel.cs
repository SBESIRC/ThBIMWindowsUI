using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCCeilingModel : GFCElementModel
    {
        public int CeilingThickness { get; set; }
        public GFCCeilingModel(ThGFC2Document gfcDoc, string matirial, int globalId, int thickness) : base(gfcDoc, matirial)
        {
            CeilingThickness = thickness;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcCeiling(gfcDoc, ref globalId, thickness, -1);
        }

        public GFCCeilingModel(ThGFC2Document gfcDoc, string matirial, DeductGFCModel room, int shapeId, int globalId, int thickness) : base(gfcDoc, matirial)
        {
            CeilingThickness = thickness;
            Model = room;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcCeiling(gfcDoc, ref globalId, thickness, shapeId);
        }
    }
}
