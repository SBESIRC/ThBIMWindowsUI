using THBimEngine.Domain.MidModel;

namespace THBimEngine.Domain.Grid
{
    public class GridCircle
    {
        PointVector center; // 圆环中心
        float radius; // 圆环半径
        PointVector normal; // 圆环朝向

        Color color; // 圆环颜色
        float width;	// 显示粗细

        public GridCircle(CircleLable circleLable)
        {
            var circle = circleLable.Circle;
            center = new PointVector()
            {
                X = (float)circle.Center.X,
                Y = (float)circle.Center.Y,
                Z = (float)circle.Center.Z
            };
            radius = (float)circle.Radius;
            normal = new PointVector()
            {
                X = 0,
                Y = 0,
                Z = 1
            };
            color = new Color(255, 0, 0, 1);
            width = 0.1f;
        }
    }
}
