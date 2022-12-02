﻿using SqlSugar;

namespace THBimEngine.DBOperation
{
    [SugarTable("AI_prjrole")]
    public class DBSubProject
    {
        /// <summary>
        /// 项目Id
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
        /// <summary>
        /// 子项Id
        /// </summary>
        public string SubentryId { get; set; }
        /// <summary>
        /// 子项名称
        /// </summary>
        public string SubEntryName { get; set; }
        public string ExecutorId { get; set; }
    }
}
