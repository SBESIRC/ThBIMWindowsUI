using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using THBimEngine.DBOperation.DBModels;

namespace THBimEngine.DBOperation
{
    public class ProjectFileHelper
    {
        ConnectionConfig dbMySqlConfig;
        public ProjectFileHelper(string connectStr) 
        {
            dbMySqlConfig = new ConnectionConfig()
            {
                ConfigId = "DBMySql",
                DbType = SqlSugar.DbType.MySql,
                ConnectionString = connectStr,
                IsAutoCloseConnection = true,
            };
        }
        public SqlSugarProvider GetDBConnect() 
        {
            var sqlClient = new SqlSugarClient(dbMySqlConfig);
            SqlSugarProvider sqlDB = sqlClient.GetConnection(dbMySqlConfig.ConfigId);
            return sqlDB;
        }
        public List<DBLink> GetProjectFileLink(List<string> prjFileIds) 
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBLink>();
            expressionable.And(c => prjFileIds.Contains(c.ProjectFileId));
            expressionable.And(c => c.IsDel == 0);
            var prjFileLinks = sqlDB.Queryable<DBLink>().Where(expressionable.ToExpression()).ToList();
            return prjFileLinks;
        }
        public List<DBProjectFile> GetDBProjectFiles(string prjId, string subPrjId, string appName, string major) 
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBProjectFile>();
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => c.SubPrjId == subPrjId);
            expressionable.And(c => c.ApplicationName == appName);
            expressionable.And(c => c.MajorName == major);
            var prjFiles = sqlDB.Queryable<DBProjectFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles;
        }
        public List<DBVAllProjectFile> GetDBProjectMainFiles(List<string> prjFileIds) 
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVAllProjectFile>();
            expressionable.And(c => c.IsMainFile == 1);
            expressionable.And(c => prjFileIds.Contains(c.ProjectFileId));
            var prjFiles = sqlDB.Queryable<DBVAllProjectFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles;
        }
        public List<DBVProjectMainFile> GetProjectFiles(string prjId,string subPrjId) 
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectMainFile>();
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => c.SubPrjId == subPrjId);
            expressionable.And(c => !string.IsNullOrEmpty(c.FileUploadId));
            var prjFiles = sqlDB.Queryable<DBVProjectMainFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles;
        }
        public List<DBVProjectMainDelFile> GetProjectDeleteFiles(string prjId, string subPrjId)
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectMainDelFile>();
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => c.SubPrjId == subPrjId);
            var prjFiles = sqlDB.Queryable<DBVProjectMainDelFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles;
        }
        public List<DBVProjectMainFile> GetProjectFiles(string prjId)
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectMainFile>();
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => !string.IsNullOrEmpty(c.FileUploadId));
            var prjFiles = sqlDB.Queryable<DBVProjectMainFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles;
        }
        public List<DBVProjectMainFile> GetProjectFiles(List<string> prjFileIds)
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectMainFile>();
            expressionable.And(c => prjFileIds.Contains(c.ProjectFileId));
            expressionable.And(c => !string.IsNullOrEmpty(c.FileUploadId));
            var prjFiles = sqlDB.Queryable<DBVProjectMainFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles;
        }
        public DBVProjectMainFile GetProjectFile(string prjFileId)
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectMainFile>();
            expressionable.And(c => c.ProjectFileId == prjFileId);
            expressionable.And(c => !string.IsNullOrEmpty(c.FileUploadId));
            var prjFiles = sqlDB.Queryable<DBVProjectMainFile>().Where(expressionable.ToExpression()).ToList();
            return prjFiles.FirstOrDefault();
        }
        public List<DBVProjectAllFile> GetProjectAllFileIds(List<string> prjMainFileIds)
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectAllFile>();
            expressionable.And(c => prjMainFileIds.Contains(c.ProjectFileId));
            var prjAllFiles = sqlDB.Queryable<DBVProjectAllFile>().Where(expressionable.ToExpression()).ToList();
            return prjAllFiles;
        }
        public List<DBVProjectFile> GetProjectAllFiles(List<string> projectFileUploadIds)
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBVProjectFile>();
            expressionable.And(c => projectFileUploadIds.Contains(c.ProjectUploadId));
            var prjAllFiles = sqlDB.Queryable<DBVProjectFile>().Where(expressionable.ToExpression()).ToList();
            return prjAllFiles;
        }
        public DBProjectFile GetHisProjectFile(string prjId, string subPrjId, string majorName, string sourceName, string fileName,string buidingName,int isDel =0) 
        {
            var sqlDB = GetDBConnect();
            var expressionable = new Expressionable<DBProjectFile>();
            expressionable.And(c => c.FileName == fileName);
            expressionable.And(c => c.PrjId == prjId);
            expressionable.And(c => c.SubPrjId == subPrjId);
            expressionable.And(c => c.MajorName == majorName);
            expressionable.And(c => c.ApplicationName == sourceName);
            expressionable.And(c => c.IsDel == isDel);
            var res = sqlDB.Queryable<DBProjectFile>().Where(expressionable.ToExpression()).ToList();
            if (res.Count < 1)
                return null;
            return res.FirstOrDefault();
        }
        public void DelHisProjectFile(string uId, string uName, string prjFileId)
        {
            DelHisProjectFile(GetDBConnect(), uId,uName, prjFileId);
        }
        public void DelHisProjectFile(SqlSugarProvider sqlDB,string uId, string uName, string prjFileId)
        {
            //删除旧数据
            sqlDB.Updateable<DBProjectFile>().SetColumns(it =>
                new DBProjectFile()
                {
                    IsDel = 1,
                    UpdateTime = DateTime.Now,
                    UpdatedBy = uId,
                    UpdatedUserName = uName,
                })
                .Where(it => it.ProjectFileId == prjFileId
                && it.IsDel == 0).ExecuteCommand();
        }
        public void UnDelHProjectFile(SqlSugarProvider sqlDB, string uId, string uName, string prjFileId)
        {
            //删除旧数据
            sqlDB.Updateable<DBProjectFile>().SetColumns(it =>
                new DBProjectFile()
                {
                    IsDel = 0,
                    UpdateTime = DateTime.Now,
                    UpdatedBy = uId,
                    UpdatedUserName = uName,
                })
                .Where(it => it.ProjectFileId == prjFileId
                && it.IsDel == 1).ExecuteCommand();
        }
        public void DelHisProjectUploadFile(string prjFileId)
        {
            //删除旧数据
            DelHisProjectAllUploadFile(GetDBConnect(), prjFileId);
        }
        public void DelHisProjectAllUploadFile(SqlSugarProvider sqlDB,string prjFileId)
        {
            //删除旧数据
            sqlDB.Updateable<DBProjectFileUpload>().SetColumns(it =>
                new DBProjectFileUpload()
                {
                    IsDel = 1,
                })
                .Where(it => it.ProjectFileId == prjFileId
                && it.IsDel == 0).ExecuteCommand();
        }
        public void DelHisProjectUploadFile(SqlSugarProvider sqlDB, string prjFileId,string fileName)
        {
            //删除旧数据
            sqlDB.Updateable<DBProjectFileUpload>().SetColumns(it =>
                new DBProjectFileUpload()
                {
                    IsDel = 1,
                })
                .Where(it => it.ProjectFileId == prjFileId
                && it.FileName == fileName
                && it.IsDel == 0).ExecuteCommand();
        }
        public void DelHisProjectUploadFile(SqlSugarProvider sqlDB, string prjFileUploadId)
        {
            //删除旧数据
            sqlDB.Updateable<DBProjectFileUpload>().SetColumns(it =>
                new DBProjectFileUpload()
                {
                    IsDel = 1,
                })
                .Where(it => it.ProjectFileUploadId == prjFileUploadId
                && it.IsDel == 0).ExecuteCommand();
        }
        public void UnDelProjectUploadFile(SqlSugarProvider sqlDB, string prjFileUploadId)
        {
            //删除旧数据
            sqlDB.Updateable<DBProjectFileUpload>().SetColumns(it =>
                new DBProjectFileUpload()
                {
                    IsDel = 0,
                })
                .Where(it => it.ProjectFileUploadId == prjFileUploadId
                && it.IsDel == 1).ExecuteCommand();
        }
        public void DelHisProjectLink(SqlSugarProvider sqlDB, string linkId)
        {
            //删除旧数据
            sqlDB.Updateable<DBLink>().SetColumns(it =>
                new DBLink()
                {
                    IsDel = 1,
                })
                .Where(it => it.LinkId == linkId
                && it.IsDel == 0).ExecuteCommand();
        }
    }
}
