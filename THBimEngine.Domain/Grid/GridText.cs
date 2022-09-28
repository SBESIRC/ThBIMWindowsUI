using System.Linq;

namespace THBimEngine.Domain.Grid
{
    public class GridText
    {
        string content;// 文字内容
        string type; // 字体
        THBimMaterial color;// 文字颜色
        float size; // 文字大小
        PointVector normal;// 文字朝向
        PointVector direction; // 文字方向
        PointVector center;// 文字位置

        public GridText(CircleLable circleLable)
        {
            content = circleLable.Mark;
            var circleCenter = circleLable.Circle.Center;
            type = "";
            color = new THBimMaterial()
            {
                Color_R = 255,
                Color_G = 0,
                Color_B = 0

            };
            size = 350;
            normal = new PointVector() { X = 0, Y = 0, Z = 1 };
            direction = new PointVector() { X = 1, Y = 0, Z = 0 };
            center = new PointVector()
            {
                X = (float)circleCenter.X,
                Y = (float)circleCenter.Y,
                Z = (float)circleCenter.Z
            };

        }
        public GridText(ThTCHLine dimLine, string dimension)
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
            direction = new PointVector() { X = 1, Y = 0, Z = 0 };
            center = GetMidPt(dimLine.StartPt,dimLine.EndPt);
        }

        private PointVector GetMidPt(ThTCHPoint3d pt1, ThTCHPoint3d pt2)
        {
            return new PointVector() 
            { 
                X = (float)((pt1.X + pt2.X) / 2),
                Y = (float)((pt1.Y + pt2.Y) / 2), 
                Z = (float)((pt1.Z + pt2.Z) / 2) 
            };
        }
    }
}
