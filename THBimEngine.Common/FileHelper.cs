using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace THBimEngine.Common
{
    public class FileHelper
    {
        public static string GetMD5ByMD5CryptoService(string path) 
        {
            if (!File.Exists(path)) return "";
            FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            MD5CryptoServiceProvider md5Provider = new MD5CryptoServiceProvider();
            byte[] buffer = md5Provider.ComputeHash(fs);
            string resule = BitConverter.ToString(buffer);
            md5Provider.Clear();
            fs.Close();
            return resule;
        }
        public static string GetMD5ByHashAlgorithm(string path)
        {
            if (!File.Exists(path)) return "";
            int bufferSize = 1024 * 16;//自定义缓冲区大小16K            
            byte[] buffer = new byte[bufferSize];
            Stream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            HashAlgorithm hashAlgorithm = new MD5CryptoServiceProvider();
            int readLength = 0;//每次读取长度            
            var output = new byte[bufferSize];
            while ((readLength = inputStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                //计算MD5                
                hashAlgorithm.TransformBlock(buffer, 0, readLength, output, 0);
            }
            //完成最后计算，必须调用(由于上一部循环已经完成所有运算，所以调用此方法时后面的两个参数都为0)            		  
            hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
            string md5 = BitConverter.ToString(hashAlgorithm.Hash);
            hashAlgorithm.Clear();
            inputStream.Close();
            return md5;
        }
        /// <summary>
        /// 获取文件夹下的所有文件
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="containChildDir"></param>
        /// <param name="toUpper"></param>
        /// <returns></returns>
        public static List<string> GetDirectoryFiles(string dirPath, bool containChildDir,bool toUpper) 
        {
            var dirFiles = new List<string>();
            if (string.IsNullOrEmpty(dirPath))
                return dirFiles;
            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(dirPath);
                //获取文件夹下的文件
                foreach (var item in directoryInfo.GetFiles()) 
                {
                    if(toUpper)
                        dirFiles.Add(item.FullName.ToUpper());
                    else
                        dirFiles.Add(item.FullName);
                }
                if (containChildDir) 
                {
                    //递归获取子文件夹的
                    foreach (var dir in directoryInfo.GetDirectories()) 
                    {
                        var tempFiles = GetDirectoryFiles(dir.FullName, containChildDir, toUpper);
                        if (tempFiles == null || tempFiles.Count < 1)
                            continue;
                        dirFiles.AddRange(tempFiles);
                    }
                }
            }
            catch { }
            return dirFiles;
        }
    }
}
