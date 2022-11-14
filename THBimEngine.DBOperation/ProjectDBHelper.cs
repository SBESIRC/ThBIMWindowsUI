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
            var dataTable = sqlClient.Ado.GetDataTable("SELECT [Id] FROM [Product_Project].[dbo].[AI_prjrole] where [ExecutorId]=@Uid", new { Uid = userId });
            var prjIds = new List<string>();
            foreach (DataRow dataRow in dataTable.Rows)
            {
                var id = dataRow[0].ToString();
                if (prjIds.Contains(id))
                    continue;
                prjIds.Add(id);
            }
            //step2根据项目获取项目的完整信息
            foreach (var prjId in prjIds)
            {
                Expressionable<DBProject> expressionable = new Expressionable<DBProject>();
                expressionable.And(c => c.Id == prjId);
                expressionable.And(c => c.MajorName == null);
                var prjs = sqlDB.Queryable<DBProject>().Where(expressionable.ToExpression()).ToList();
                if (prjs.Count < 1)
                    continue;
                var prj = prjs.FirstOrDefault();
                prj.SubProjects = new List<DBSubProject>();
                Expressionable<DBSubProject> subExp = new Expressionable<DBSubProject>();
                subExp.And(c => c.Id == prjId);
                var subPrjs = sqlDB.Queryable<DBSubProject>().Where(subExp.ToExpression()).Distinct().ToList();
                prj.SubProjects.AddRange(subPrjs);
                resPorject.Add(prj);
            }
            return resPorject;
        }
    }
}
