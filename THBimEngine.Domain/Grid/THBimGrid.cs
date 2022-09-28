using System;
using System.Collections.Generic;
using System.Linq;

namespace THBimEngine.Domain.Grid
{
    public class THBimGrid : THBimEntity
    {
        public List<GridLine> GridLines = new List<GridLine>();
        public List<GridCircle> GridCircles = new List<GridCircle>();
        public List<GridText> GridTexts = new List<GridText>();

        public THBimGrid(ThTCHPolyline gridLine) : base(0, "", "", null, "", "")
        {
            GridLines.Add(new GridLine(gridLine));
        }

        public THBimGrid(CircleLable circleLable) : base(0, "", "", null, "", "")
        {
            GridLines.Add(new GridLine(circleLable));
            GridCircles.Add(new GridCircle(circleLable));
            GridTexts.Add(new GridText(circleLable));
        }

        public THBimGrid(ThAlignedDimension dimensionData) : base(0, "", "", null, "", "")
        {
            var dimLines = dimensionData.DimLines;
            var dimension = dimensionData.Mark;
            for (int i =0; i < dimLines.Count;i++)
            {
                var dimLine = dimLines[i];
                GridLines.Add(new GridLine(dimLine));
                if(i==dimLines.Count-1)
                {
                    GridTexts.Add(new GridText(dimLine, dimension));
                }
            }
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
