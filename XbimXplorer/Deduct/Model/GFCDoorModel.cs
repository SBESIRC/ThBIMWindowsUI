using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCDoorModel : GFCElementModel
    {
        public double DoorHeight { get; set; }
        public double DoorLength { get; set; }

        public GFCDoorModel(ThGFC2Document gfcDoc, string matirial, int globalId, double doorLength, double height) : base(gfcDoc, matirial)
        {
            DoorHeight = height;
            DoorLength = doorLength;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcDoor(gfcDoc, -1, true, doorLength, height, 0, ref globalId);
        }

        public GFCDoorModel(ThGFC2Document gfcDoc, string matirial, DeductGFCModel door, int shapeId, bool isModel, int globalId, double doorLength, double height, double aboveFloorHeight) : base(gfcDoc, matirial)
        {
            DoorHeight = height;
            DoorLength = doorLength;
            IsConstruct = false;
            Model = door;
            ID = THModelToGFC2.ToGfcDoor(gfcDoc, shapeId, false, doorLength, height, aboveFloorHeight, ref globalId);
        }
    }
}
