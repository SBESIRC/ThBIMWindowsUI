using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common.Geometry;

using THBimEngine.IO.GFC2;

namespace XbimXplorer.Deduct.Model
{
    public class GFCDoorModel : GFCElementModel
    {
        public int DoorHeight { get; set; }
        public int DoorLength { get; set; }

        public GFCDoorModel(ThGFC2Document gfcDoc, int globalId, string name, int doorLength, int doorHeight) : base(gfcDoc, globalId, name)
        {
            DoorHeight = doorHeight;
            DoorLength = doorLength;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcDoor(gfcDoc, globalId, name, -1, doorLength, doorHeight, 0);
        }

        public GFCDoorModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel door, double wallGlobalZ) : base(gfcDoc, globalId, name)
        {
            var doorLength = (int)Math.Round(door.CenterLine.Length);
            var doorHeight = (int)Math.Round(door.ZValue);
            var aboveFloorHeight = door.GlobalZ - wallGlobalZ;

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wallGlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location); //高度

            var interPt = door.CenterLine.MidPoint;
            var interPtId = gfcDoc.AddGfc2Vector2d(interPt.X, interPt.Y); //2d的投影，中点
            var baseInterPtId = gfcDoc.AddGfc2Vector2d(0, -doorHeight / 2); //高度,下边界为原点
            var polyId = gfcDoc.AddSimpolyPolygon(doorLength, doorHeight);//长,高/2的四边形
            var shapeId = gfcDoc.AddSectionPointShape(localCoordinateId, interPtId, baseInterPtId, polyId);
            name = "";

            DoorHeight = doorHeight;
            DoorLength = doorLength;
            IsConstruct = false;
            Model = door;

            ID = THModelToGFC2.ToGfcDoor(gfcDoc, globalId, name, shapeId, doorLength, doorHeight, aboveFloorHeight);
        }

        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCDoorModel>().First(o => o.DoorHeight == current.DoorHeight && o.DoorLength == current.DoorLength);
            construct.Primitives.Add(current);
        }

    }
}
