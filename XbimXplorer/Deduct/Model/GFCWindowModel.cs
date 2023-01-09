using System;
using System.Collections.Generic;
using System.Linq;

using Xbim.Common.Geometry;

using THBimEngine.IO.GFC2;


namespace XbimXplorer.Deduct.Model
{
    public class GFCWindowModel : GFCElementModel
    {
        public int WindowHeight { get; set; }
        public int WindowLength { get; set; }

        public GFCWindowModel(ThGFC2Document gfcDoc, int globalId, string name, int windowLength, int windowHeight) : base(gfcDoc, globalId, name)
        {
            WindowHeight = windowHeight;
            WindowLength = windowLength;
            IsConstruct = true;
            ID = THModelToGFC2.ToGfcWindow(gfcDoc, globalId, name, -1, windowLength, windowHeight, 0);
        }

        public GFCWindowModel(ThGFC2Document gfcDoc, int globalId, string name, DeductGFCModel window, double wallGlobalZ) : base(gfcDoc, globalId, name)
        {
            var windowLength = (int)Math.Round(window.CenterLine.Length);
            var windowHeight = (int)Math.Round(window.ZValue);
            var aboveFloorHeight = window.GlobalZ - wallGlobalZ;

            var location = new XbimMatrix3D(new XbimVector3D(0, 0, wallGlobalZ));
            var localCoordinateId = gfcDoc.AddGfc2Coordinates3d(location); //墙的高度

            var interPt = window.CenterLine.MidPoint;
            var interPtId = gfcDoc.AddGfc2Vector2d(interPt.X, interPt.Y); //2d的投影，中点
            var baseInterPtId = gfcDoc.AddGfc2Vector2d(0, -windowHeight / 2); //高度,下边界为原点
            var polyId = gfcDoc.AddSimpolyPolygon(windowLength, windowHeight);//长,高/2的四边形
            var shapeId = gfcDoc.AddSectionPointShape(localCoordinateId, interPtId, baseInterPtId, polyId);
            name = "";

            WindowHeight = windowHeight;
            WindowLength = windowLength;
            IsConstruct = false;
            Model = window;

            ID = THModelToGFC2.ToGfcWindow(gfcDoc, globalId, name, shapeId, windowLength, windowHeight, aboveFloorHeight);
        }

        public override void AddGFCItemToConstruct(List<GFCElementModel> constructList)
        {
            var current = this;
            var construct = constructList.OfType<GFCWindowModel>().First(o => o.WindowHeight == current.WindowHeight && o.WindowLength == current.WindowLength);
            construct.Primitives.Add(current);
        }

    }
}
