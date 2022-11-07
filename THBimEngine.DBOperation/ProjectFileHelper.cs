using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace THBimEngine.DBOperation
{
    class ProjectFileHelper
    {
        public void InsertFileUploadResult(DBFile file)
        {
            string sql = string.Format("insert into FileUpload(FileUrl,FileRealName,Uploader,UploaderName) " +
                "values('{0}','{1}','{2}','{3}')", file.FileUrl, file.FileRealName, file.Uploader, file.UploaderName);
             MySqlHelper.ExecuteNonQuery(sql);
            sql = "select max(Id) from FileUpload";
            var res = MySqlHelper.ExecuteScalar(sql);
            int.TryParse(res.ToString(), out int newId);
            file.Id = newId;
        }
        public void InsertProjectFileUploadResult(DBProjectFileUpload projectFile)
        {
            string sql = string.Format("insert into ProjectFileUpload(PrjFileId,FileUploadId) " +
                "values('{0}','{1}')", projectFile.PrjFileId, projectFile.FileInfo.Id);
            MySqlHelper.ExecuteNonQuery(sql);
            sql = "select max(Id) from FileUpload";
            var res = MySqlHelper.ExecuteScalar(sql);
            int.TryParse(res.ToString(), out int newId);
            projectFile.Id = newId;
        }
        public void InsertProjectFile(DBProjectFile projectFile)
        {
            string sql = string.Format("insert into ProjectFiles(PrjNum,SubPrjNum,FileName,SystemType,MajorName,Creater,CreaterName) " +
                "values('{0}','{1}','{2}','{3}')", projectFile.PrjNum, projectFile.SubPrjNum, projectFile.FileRealName, projectFile.SystemType, projectFile.MajorName, projectFile.Creater, projectFile.CreaterName);
            MySqlHelper.ExecuteNonQuery(sql);
            //获取插入后的最新Id
            sql = "select max(Id) from ProjectFiles";
            var res = MySqlHelper.ExecuteScalar(sql);
            int.TryParse(res.ToString(), out int newId);
            projectFile.Id = newId;
        }
        public void ProjectFile(DBProjectFile projectFile) 
        {
            InsertProjectFile(projectFile);
            projectFile.FileUpload.PrjFileId = projectFile.Id;
            InsertFileUploadResult(projectFile.FileUpload.FileInfo);
            InsertProjectFileUploadResult(projectFile.FileUpload);

        }
    }
}
