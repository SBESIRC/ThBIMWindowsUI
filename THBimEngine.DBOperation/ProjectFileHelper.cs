using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.DBOperation
{
    public class ProjectFileHelper
    {
        ConnectionConfig dbMySqlConfig = new ConnectionConfig()
        {
            ConfigId = "DBMySql",
            DbType = SqlSugar.DbType.MySql,
            ConnectionString = string.Format("Server={0};Port={1};Database={2};Uid={3};Pwd={4};", "172.17.1.37", "3306", "thbim_project", "thbim_project", "5Z_7e6B8d54b"),
            IsAutoCloseConnection = true,
        };
        public List<DBProjectFile> GetProjectFiles(string prjId,string subPrjId) 
        {
            var sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            var expressionable = new Expressionable<DBProjectFile>();
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => c.SubPrjId == subPrjId);
            expressionable.And(c => c.IsDel == 0);
            var prjFiles = sqlDB.Queryable<DBProjectFile>().Where(expressionable.ToExpression()).ToList();
            //foreach(var item in prjFiles)
            //{
            //    //获取相应的文件信息
            //    item.FileUploads = new List<DBProjectFileUpload>();
            //    var expFile = new Expressionable<DBProjectFileUpload>();
            //    expFile.And(c => c.IsDel == 0);
            //    expFile.And(c=>c.ProjectFileId == )
            //}
            return prjFiles;
        }
        public bool InsertFileUploadResult(DBFile file)
        {
            SqlSugarClient sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            //插入新数据
            int res = sqlDB.Insertable(file).ExecuteCommand();
            return res > 0;

        }
        public bool InsertProjectFileUploadResult(DBProjectFileUpload projectFile)
        {
            SqlSugarClient sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            //插入新数据
            int res = sqlDB.Insertable(projectFile).ExecuteCommand();
            return res > 0;
        }
        public bool InsertProjectFile(DBProjectFile projectFile)
        {
            SqlSugarClient sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            //插入新数据
            int res = sqlDB.Insertable(projectFile).ExecuteCommand();
            return res > 0;
        }
    }
}
