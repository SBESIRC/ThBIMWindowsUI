﻿using System.Linq;
using THBimEngine.Domain.MidModel;

namespace THBimEngine.Domain.Grid
{
    public class GridLine
    {
        PointVector stPt = new PointVector();
        PointVector edPt;
        Color color;
        float width; //线宽
        string type; // 线型

        public GridLine(ThTCHPolyline gridLine)
        {
            var spt = gridLine.Points.First();
            var ept = gridLine.Points.Last();
            stPt = new PointVector()
            {
                X = (float)spt.X,
                Y = (float)spt.Y,
                Z = (float)spt.Z
            };
            edPt = new PointVector()
            {
                X = (float)ept.X,
                Y = (float)ept.Y,
                Z = (float)ept.Z
            };
            color = new Color(255, 0, 0, 1);
            width = 0.1f;
            type = "DASH";
        }

        public GridLine(CircleLable circleLable)
        {
            var line = circleLable.Leaders.First();

            var spt = line.StartPt;
            stPt = new PointVector()
            {
                X = (float)line.StartPt.X,
                Y = (float)line.StartPt.Y,
                Z = (float)line.StartPt.Z
            };
            edPt = new PointVector()
            {
                X = (float)line.EndPt.X,
                Y = (float)line.EndPt.Y,
                Z = (float)line.EndPt.Z
            };
            color = new Color(255, 0, 0, 1);
            width = 0.1f;
            type = "DASH";
        }


        public GridLine(ThTCHLine dimLine)
        {
            stPt = new PointVector()
            {
                X = (float)dimLine.StartPt.X,
                Y = (float)dimLine.StartPt.Y,
                Z = (float)dimLine.StartPt.Z
            };
            edPt = new PointVector()
            {
                X = (float)dimLine.EndPt.X,
                Y = (float)dimLine.EndPt.Y,
                Z = (float)dimLine.EndPt.Z
            };


            color = new Color(255, 0, 0, 1);
            width = 0.1f;
            type = "DASH";
        }
    }
}