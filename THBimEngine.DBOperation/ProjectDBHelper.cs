using SqlSugar;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace THBimEngine.DBOperation
{
    public class ProjectDBHelper
    {
        ConnectionConfig dbSqlServerConfig = new ConnectionConfig()
        {
            ConfigId = "DBSqlServer",
            DbType = SqlSugar.DbType.SqlServer,
            ConnectionString = "Data Source= 172.16.0.2;Initial Catalog=Product_Project;User ID=ghost123;Password=12345abc123!",
            IsAutoCloseConnection = true,
        };
        public List<DBProject> GetUserProjects(string userId)
        {
            var resPorject = new List<DBProject>();
            if (string.IsNullOrEmpty(userId))
                return resPorject;
            SqlSugarClient sqlClient = new SqlSugarClient(dbSqlServerConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbSqlServerConfig.ConfigId);
            //step1获取可以看的项目
            Expressionable<DBProject> expressionable = new Expressionable<DBProject>();
            expressionable.And(c => c.ExecutorId == userId);
            var prjs = sqlDB.Queryable<DBProject>().Where(expressionable.ToExpression()).Distinct().ToList();
            var prjIds = prjs.Select(c => c.Id).ToList();
            Expressionable<DBSubProject> expSubPrj = new Expressionable<DBSubProject>();
            expSubPrj.And(c => prjIds.Contains(c.Id));
            var allSubPrjs = sqlDB.Queryable<DBSubProject>().Where(expSubPrj.ToExpression()).Distinct().ToList();
            //step2根据项目获取项目的完整信息
            foreach (var prj in prjs)
            {
                prj.ExecutorId = "";
                prj.SubProjects = new List<DBSubProject>();
                var subPrjs = allSubPrjs.Where(c => c.Id == prj.Id).Distinct();
                prj.SubProjects.AddRange(subPrjs);
                resPorject.Add(prj);
            }
            return resPorject;
        }
    }
}
