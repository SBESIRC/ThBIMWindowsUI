using System;

namespace THBimEngine.Domain.Grid
{
    public class GridText:THBimEntity
    {
        public string content;
        public string type;
        public THBimMaterial color;
        public float size;
        public PointVector normal;
        public PointVector direction;
        public PointVector center;

        public GridText(CircleLable circleLable, double elevation=0) : base(0, "", "", null)
        {
            content = circleLable.Mark;
            var circleCenter = circleLable.Circle.Center;
            type = "";
            color = new THBimMaterial()
            {
                Color_R = 1.0f,
                Color_G = 0,
                Color_B = 0
            };
            size = (float)(circleLable.Circle.Radius / 0.75);
            normal = new PointVector() { X = 0, Y = 0, Z = 1 };
            direction = new PointVector() { X = 1, Y = 0, Z = 0 };
            center = new PointVector()
            {
                X = (float)circleCenter.X,
                Y = (float)circleCenter.Y,
                Z = (float)elevation
            };
        }

        public GridText(ThTCHLine dimLine, string dimension, double elevation = 0) : base(0, "", "", null)
        {
            content = dimension;
            type = "";
            color = new THBimMaterial()
            {
                Color_R = 255,
                Color_G = 0,
                Color_B = 0
            };
            size = 350;
            normal = new PointVector() { X = 0, Y = 0, Z = 1 };
            center = GetMidPt(dimLine.StartPt, dimLine.EndPt, elevation);
            var spt = dimLine.StartPt;
            var ept = dimLine.EndPt;
            if (spt.X > ept.X + 1 || (Math.Abs(spt.X - ept.X) < 1 && spt.Y > ept.Y))
            {
                Swap(ref spt, ref ept);
            }
            direction = new PointVector() { X = (float)(ept.X-spt.X), Y = (float)(ept.Z - spt.Z), Z = (float)(ept.Y - spt.Y) };
        }

        private void Swap(ref ThTCHPoint3d spt,ref ThTCHPoint3d ept)
        {
            var temp = new ThTCHPoint3d(spt);
            spt = new ThTCHPoint3d(ept);
            ept = new ThTCHPoint3d(temp);
        }

        private PointVector GetMidPt(ThTCHPoint3d pt1, ThTCHPoint3d pt2, double elevation=0)
        {
            return new PointVector() 
            { 
                X = (float)((pt1.X + pt2.X) / 2),
                Y = (float)((pt1.Y + pt2.Y) / 2), 
                Z = (float)elevation
            };
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
