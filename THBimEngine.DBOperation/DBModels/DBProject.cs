using System;
using System.Collections.Generic;

namespace THBimEngine.DBOperation
{
    public class DBProject
    {
        public string Id { get; set; }
        public string PrjNo { get; set; }
        public string PrjName { get; set; }
        public string DesignTypeName { get; set; }
        public DateTime CreateTime { get; set; }
        public string ExecutorId { get; set; }
        public string ExecutorName { get; set; }
        public List<DBSubProject> SubProjects { get; set; }
    }
}
