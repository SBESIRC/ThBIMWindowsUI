using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCSlabModel : GFCElementModel
    {
        public int SlabThickness { get; set; }
        public GFCSlabModel(ThGFC2Document gfcDoc, string matirial, int globalId, string storeyName, int thickness) : base(gfcDoc, matirial)
        {
            SlabThickness = thickness;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcSlabModel(gfcDoc, ref globalId, storeyName, thickness);
        }

        public GFCSlabModel(ThGFC2Document gfcDoc, string matirial, DeductGFCModel slab, int shapeId, int globalId, int thickness, string name) : base(gfcDoc, matirial)
        {
            SlabThickness = thickness;
            Model = slab;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcSlab(gfcDoc, ref globalId, name, thickness, shapeId);
        }
    }
}
