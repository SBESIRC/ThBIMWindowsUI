using THBimEngine.Domain.GeneratorModel;

namespace THBimEngine.Domain.Grid
{
    public class GridCircle: THBimEntity
    {
        public PointVector center; 
        public float radius;
        public PointVector normal;
        public Color color;
        public float width;

        public GridCircle(CircleLable circleLable,double elevation=0) : base(0, "", "", null)
        {
            var circle = circleLable.Circle;
            center = new PointVector()
            {
                X = (float)circle.Center.X,
                Y = (float)circle.Center.Y,
                Z = (float)elevation
            };
            radius = (float)circle.Radius;
            normal = new PointVector()
            {
                X = 0,
                Y = 0,
                Z = 1
            };
            color = new Color(0, 1, 0, 1);
            width = 0.1f;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}
