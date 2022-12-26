using SqlSugar;
using System;

namespace THBimEngine.DBOperation.DBModels
{
    [SugarTable("ProjectFileLink")]
    public class DBLink
    {
        public string LinkId { get; set; }
        public string FromLinkId { get; set; }
        public string ProjectFileId { get; set; }
        public string LinkProjectFileId { get; set; }
        public double LinkMoveX { get; set; }
        public double LinkMoveY { get; set; }
        public double LinkMoveZ { get; set; }
        public double LinkRotainAngle { get; set; }
        public string LinkerId { get; set; }
        public string LinkerName { get; set; }
        public DateTime LinkTime { get; set; }
        public int LinkState { get; set; }
        public int IsDel { get; set; }
    }
}
