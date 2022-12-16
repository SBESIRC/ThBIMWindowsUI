using SqlSugar;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace THBimEngine.DBOperation
{
    public class ProjectDBHelper
    {
        ConnectionConfig dbSqlServerConfig;
        public ProjectDBHelper(string sqlIp,string location) 
        {
            string sqlIP = "172.16.0.2";
            if (!string.IsNullOrEmpty(sqlIp) )
            {
                sqlIP = sqlIp;
            }
            if (location.Contains("广州"))
            {
                dbSqlServerConfig = new ConnectionConfig()
                {
                    ConfigId = "DBSqlServer",
                    DbType = SqlSugar.DbType.SqlServer,
                    ConnectionString = string.Format("Data Source= {0};User ID=ghost;Password=12345abc123!", sqlIP),
                    IsAutoCloseConnection = true,
                };
            }
            else 
            {
                dbSqlServerConfig = new ConnectionConfig()
                {
                    ConfigId = "DBSqlServer",
                    DbType = SqlSugar.DbType.SqlServer,
                    ConnectionString = string.Format("Data Source= {0};User ID=ghost123;Password=12345abc123!", sqlIP),
                    IsAutoCloseConnection = true,
                };
            }
            
        }
        public List<DBProject> GetUserProjects(string userId)
        {
            var resPorject = new List<DBProject>();
            if (string.IsNullOrEmpty(userId))
                return resPorject;
            SqlSugarClient sqlClient = new SqlSugarClient(dbSqlServerConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbSqlServerConfig.ConfigId);
            //step1获取可以看的项目
            Expressionable<DBSubProject> expressionable = new Expressionable<DBSubProject>();
            expressionable.And(c => c.ExecutorId == userId);
            var prjs = sqlDB.Queryable<DBSubProject>().Where(expressionable.ToExpression()).Distinct().ToList();
            if (prjs.Count < 1)
                return resPorject;
            HashSet<string> hisPrjId = new HashSet<string>();
            HashSet<string> hisSubPrjId = new HashSet<string>();
            foreach (var item in prjs)
            {
                DBProject pPrj = null;
                if (hisPrjId.Contains(item.Id))
                    pPrj = resPorject.Where(c => c.Id == item.Id).FirstOrDefault();
                else
                {
                    pPrj = new DBProject();
                    pPrj.Id = item.Id;
                    pPrj.PrjName = item.PrjName;
                    pPrj.PrjNo = item.PrjNo;
                    pPrj.ExecutorId = item.ExecutorId;
                    pPrj.SubProjects = new List<DBSubProject>();
                    hisPrjId.Add(item.Id);
                    resPorject.Add(pPrj);
                }
                if (hisSubPrjId.Contains(item.SubentryId))
                    continue;
                pPrj.SubProjects.Add(item);
            }
            return resPorject;
        }
    }
}
