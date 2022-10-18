using System.Linq;
using THBimEngine.Domain.MidModel;

namespace THBimEngine.Domain.Grid
{
    public class GridLine : THBimEntity
    {
        public PointVector stPt;
        public PointVector edPt;
        public Color color;
        public float width;
        public int type;

        public GridLine(ThTCHPolyline gridLine, int colorType, double elevation = 0) : base(0, "", "", null)
        {
            var spt = gridLine.Points.First();
            var ept = gridLine.Points.Last();
            stPt = new PointVector()
            {
                X = (float)spt.X,
                Y = (float)spt.Y,
                Z = (float)elevation
            };
            edPt = new PointVector()
            {
                X = (float)ept.X,
                Y = (float)ept.Y,
                Z = (float)elevation
            };
            if(colorType==1) color = new Color(1, 0, 0, 1);
            if(colorType == 2) color = new Color(0, 1, 0, 1);
            width = 0.1f;
            type = -1;
        }

        public GridLine(CircleLable circleLable, double elevation = 0) : base(0, "", "", null)
        {
            var line = circleLable.Leaders.First();
            stPt = new PointVector()
            {
                X = (float)line.StartPt.X,
                Y = (float)line.StartPt.Y,
                Z = (float)elevation
            };
            edPt = new PointVector()
            {
                X = (float)line.EndPt.X,
                Y = (float)line.EndPt.Y,
                Z = (float)elevation
            };
            color = new Color(0, 1, 0, 1);
            width = 0.1f;
            type = 1;
        }

        public GridLine(ThTCHLine dimLine, double elevation = 0) : base(0, "", "", null)
        {
            stPt = new PointVector()
            {
                X = (float)dimLine.StartPt.X,
                Y = (float)dimLine.StartPt.Y,
                Z = (float)elevation
            };
            edPt = new PointVector()
            {
                X = (float)dimLine.EndPt.X,
                Y = (float)dimLine.EndPt.Y,
                Z = (float)elevation
            };
            color = new Color(0, 1, 0, 1);
            width = 0.1f;
            type = 1;
        }

        public override object Clone()
        {
            throw new System.NotImplementedException();
        }
    }
}
