using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCRoomModel : GFCElementModel
    {
        public string RoomName { get; set; }
        public GFCRoomModel(ThGFC2Document gfcDoc, string matirial, int globalId, string name) : base(gfcDoc, matirial)
        {
            RoomName = name;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcRoom(gfcDoc, ref globalId, name, "层底标高", -1);
        }

        public GFCRoomModel(ThGFC2Document gfcDoc, string matirial, DeductGFCModel room, int shapeId, int globalId, string name, string btmElevS) : base(gfcDoc, matirial)
        {
            RoomName = name;
            Model = room;
            IsConstruct = false;
            ID = THModelToGFC2.ToGfcRoom(gfcDoc, ref globalId, name, btmElevS, shapeId);
        }
    }
}
