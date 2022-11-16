using SqlSugar;
using System;
using System.Collections.Generic;

namespace THBimEngine.DBOperation
{
    [SugarTable("[AI_prjrole]")]
    public class DBProject
    {
        /// <summary>
        /// 项目ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 项目编号
        /// </summary>
        public string PrjNo { get; set; }
        /// <summary>
        /// 项目名称
        /// </summary>
        public string PrjName { get; set; }
        public string ExecutorId { get; set; }
        /// <summary>
        /// 项目子项信息
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<DBSubProject> SubProjects { get; set; }
    }
}
