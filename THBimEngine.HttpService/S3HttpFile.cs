using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.IO;

namespace THBimEngine.HttpService
{
    public class S3HttpFile
    {
        AmazonS3Config s3Config = null;
        AmazonS3Client s3Client = null;
        private string url = "fis.thape.com.cn:9020";
        private string accessKey = @"1_THAPE\s3user2_accid";
        private string secretKey = "_6ZCAyAShoualzWy9kg22ceYu9oa";
        private string bucketname = "test";
        public S3HttpFile() 
        {
            
        }
        public void UploadFile(string localFilePath, string osskey) 
        {
               FileStream fileStream = null;
            try
            {
                if (null == s3Client)
                    InitClient(osskey);
                PutObjectRequest request = new PutObjectRequest();
                //request.ContentType = "text/plain";
                request.BucketName = bucketname;
                request.Key = string.Format("{0}/{1}/{2}",url,bucketname,osskey);
                //request.FilePath = localFilePath;
                //request.AutoCloseStream = true;
                fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                request.InputStream = fileStream;
                //request.BucketKeyEnabled = true;
                //request.AutoResetStreamPosition = true;
                //request.Metadata.Add("x-amz-meta-title", "putdata");

                //TransferUtility utility = new TransferUtility(s3Client);
                //TransferUtilityUploadRequest request = new TransferUtilityUploadRequest();
                //request.BucketName = bucketname;
                //request.Key = osskey;
                //request.InputStream = fileStream;
                //request.AutoCloseStream = true;
                request.CannedACL = S3CannedACL.PublicRead;
                //request.ContentType = "application/octet-stream";
                ////request.FilePath = localFilePath;
                //utility.Upload(request);
                PutObjectResponse response = s3Client.PutObject(request);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally 
            {
                if (null != fileStream)
                {
                    fileStream.Dispose();
                    fileStream = null;
                }
                if (null != s3Client) 
                {
                    s3Client.Dispose();
                    s3Client = null;
                }
            }
            
            

        }
        public void DownloadFile(string osskey, string loacFileName) 
        {
            try
            {
                if (null == s3Client)
                    InitClient(osskey);
                GetObjectRequest request = new GetObjectRequest();
                request.BucketName = bucketname;
                request.Key = osskey;
                GetObjectResponse response = s3Client.GetObject(request);
                response.WriteResponseStreamToFile(loacFileName);
            }
            catch (Exception ex) 
            {
                throw ex;
            }
            finally 
            {
                if (null != s3Client)
                {
                    s3Client.Dispose();
                    s3Client = null;
                }
            }
            
        }
        public void DelectFile(string osskey) 
        {
            try
            {
                if (null == s3Client)
                    InitClient(osskey);
            }
            catch (Exception ex) 
            {
            }
            finally
            {
            
            }
        }
        void InitClient(string ossKey) 
        {
            //var testClient = new AmazonS3Client(accessKey, accessKey, new AmazonS3Config());
            
            //var test = new GetBucketLocationRequest
            //{
            //    BucketName = bucketname,
            //};
            //var location =testClient.GetBucketLocation(test);
            s3Config = new AmazonS3Config();
            s3Config.ServiceURL = url;
            s3Config.UseHttp = false;
            s3Config.ForcePathStyle = true;
            s3Config.RegionEndpoint = RegionEndpoint.USEast1;
            s3Config.TcpKeepAlive.Enabled = false;
            //s3Config.UseAccelerateEndpoint = true;
            s3Config.UseArnRegion = true;
            //var test = RegionEndpoint.GetBySystemName(url);
            //s3Config.RegionEndpoint = RegionEndpoint.EUWest1;
            //s3Client = new AmazonS3Client(accessKey, secretKey, RegionEndpoint.USEast1);
            s3Client = new AmazonS3Client(accessKey, secretKey, s3Config);
        }
    }
}
