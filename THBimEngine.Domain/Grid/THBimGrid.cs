using System;
using System.Collections.Generic;

namespace THBimEngine.Domain.Grid
{
    internal class THBimGrid : THBimEntity
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

        public THBimGrid(ThDimensionGroupData dimensionData) : base(0, "", "", null, "", "")
        {
            GridLines.Add(new GridLine(dimensionData));
            GridTexts.Add(new GridText(dimensionData));
        }

        public override object Clone()
        {
            throw new NotImplementedException();
        }
    }
}
