using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using THBimEngine.Common;

namespace THBimEngine.DBOperation
{
    public class ProjectDBHelper
    {
        string sqlConnect = "Data Source= 172.16.0.2;Initial Catalog=Product_Project;User ID=ghost123;Password=12345abc123!";
        public void ConnectToDB() 
        {
            string testSql = "SELECT * FROM [Product_Project].[dbo].[AI_project] project where project.MajorCode is null order by project.CreateTime desc ";
            var dataSet = SqlHelper.ExecuteDataset(sqlConnect, System.Data.CommandType.Text, testSql);
        }
        public List<DBProject> GetUserProjects(string userId) 
        {
            var resPorject = new List<DBProject>();
            if (string.IsNullOrEmpty(userId))
                return resPorject;
            //step1获取可以看的项目
            var prjIds = UserPrjIds(userId);
            //step2根据项目获取项目的完整信息
            foreach (var prjId in prjIds) 
            {
                string sql = "SELECT [Id],[PrjNo],[PrjName],[DesignTypeName],[CreateTime] FROM [Product_Project].[dbo].[AI_project] where [Id]='"+prjId+"' and [MajorName] is null";
                var prjs = SqlToList<DBProject>(sql);
                if (prjs.Count < 1)
                    continue;
                var prj = prjs.First();
                prj.SubProjects = new List<DBSubProject>();
                var subPrjs = GetUserSubProject(prj.PrjNo);
                prj.SubProjects.AddRange(subPrjs);
                resPorject.Add(prj);
            }
            return resPorject;
        }
        public List<DBSubProject> GetUserSubProject(string prjNo) 
        {
            var subPrjs = new List<DBSubProject>();
            string sql = "SELECT [subentryid],[SubEntryName],[Id] FROM [Product_Project].[dbo].[AI_prjrole] where [PrjNo]='" + prjNo + "'";
            var res = SqlToList<DBSubProject>(sql);
            var allSubIds = res.Select(c => c.SubentryId).Distinct().ToList();
            foreach (var item in allSubIds) 
            {
                subPrjs.Add(res.Find(c => c.SubentryId == item));
            }
            return subPrjs;
        }
        private List<string> UserPrjIds(string userId) 
        {
            var prjIds = new List<string>();
            string sql = "SELECT [Id] FROM [Product_Project].[dbo].[AI_prjrole] where [ExecutorId]='" + userId + "'";
            //step2根据项目获取项目的完整信息
            var dataSet = SqlHelper.ExecuteDataset(sqlConnect, System.Data.CommandType.Text, sql);
            if (dataSet.Tables.Count < 1)
                return prjIds;
            foreach (DataTable table in dataSet.Tables)
            {
                foreach (DataRow dataRow in table.Rows)
                {
                    var id = dataRow[0].ToString();
                    if (prjIds.Contains(id))
                        continue;
                    prjIds.Add(id);
                }
            }
            return prjIds;
        }

        private List<T> SqlToList<T>(string sql) where T:class
        {
            List<T> res = new List<T>();
            var dataSet = SqlHelper.ExecuteDataset(sqlConnect, System.Data.CommandType.Text, sql);
            if (dataSet.Tables.Count < 1)
                return res;
            foreach (DataTable table in dataSet.Tables)
            {
                var dataJson = JsonHelper.ToJson(table);
                var resList = JsonHelper.DeserializeJsonToList<T>(dataJson);
                if (resList.Count < 1)
                    continue;
                res.AddRange(resList);
            }
            return res;
        }
    }
}
