using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using THBimEngine.Common;

namespace THBimEngine.HttpService
{
    public class UserLoginService
    {
        string loginUrl = "https://sso.thape.com.cn/users/sign_in";
        string infoUrl = "https://cybros.thape.com.cn/api/me";
        string JWTAUD = "";
        public UserLoginService(string jwtaud) 
        {
            JWTAUD = jwtaud;
        }
        public UserInfo UserLoginByNamePsw(string uName, string uPsw) 
        {
            var loginRes = UserLogin(uName, uPsw);
            if (string.IsNullOrEmpty(loginRes.Body))
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            UserLoginRes userLogin;
            try
            {
                userLogin = JsonHelper.DeserializeJsonToObject<UserLoginRes>(loginRes.Body);
            }
            catch (Exception ex) 
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            if(userLogin == null || string.IsNullOrEmpty(userLogin.Token))
                throw new Exception("用户登录失败，请检查用户名密码");
            var userInfoRes = UserInfo(userLogin.Token);
            if (string.IsNullOrEmpty(userInfoRes.Body))
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            UserInfo userInfo;
            try
            {
                userInfo = JsonHelper.DeserializeJsonToObject<UserInfo>(userInfoRes.Body);
            }
            catch (Exception ex)
            {
                throw new Exception("用户登录失败，请检查用户名密码");
            }
            userInfo.UserLogin = userLogin;
            return userInfo;
        }
        private HttpResponseParameter UserLogin(string uName,string uPsw) 
        {
            Encoding encoding = Encoding.UTF8;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(loginUrl, UriKind.RelativeOrAbsolute));
            webRequest.Method = "POST";
            webRequest.KeepAlive = false;
            webRequest.AllowAutoRedirect = false;
            webRequest.ProtocolVersion = HttpVersion.Version11;
            webRequest.CookieContainer = new CookieContainer();
            SetHand(webRequest);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            StringBuilder data = new StringBuilder(string.Empty);
            string body = "{\"user\":{\"username\":\"" + uName + "\",\"password\":\""+ uPsw + "\"}}";
            byte[] bytePosts = encoding.GetBytes(body);
            webRequest.ContentLength = bytePosts.Length;
            using (Stream requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(bytePosts, 0, bytePosts.Length);
                requestStream.Close();
            }
            return SetResponse(webRequest, encoding);
        }
        private HttpResponseParameter UserInfo(string token) 
        {
            Encoding encoding = Encoding.UTF8;
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(infoUrl, UriKind.RelativeOrAbsolute));
            webRequest.Method = "OPTIONS";
            webRequest.KeepAlive = false;
            webRequest.AllowAutoRedirect = false;
            SetHand(webRequest);
            webRequest.Headers.Add("Authorization", string.Format("Bearer {0}", token));
            webRequest.ProtocolVersion = HttpVersion.Version11;
            webRequest.CookieContainer = new CookieContainer();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            StringBuilder data = new StringBuilder(string.Empty);
            string body = "{}";
            byte[] bytePosts = encoding.GetBytes(body);
            webRequest.ContentLength = bytePosts.Length;
            using (Stream requestStream = webRequest.GetRequestStream())
            {
                requestStream.Write(bytePosts, 0, bytePosts.Length);
                requestStream.Close();
            }
            return SetResponse(webRequest, encoding);
        }
        /// <summary>
        /// ssl/https请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
        HttpResponseParameter SetResponse(HttpWebRequest webRequest, Encoding encoding)
        {
            HttpResponseParameter responseParameter = new HttpResponseParameter();
            using (HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse())
            {
                responseParameter.Uri = webResponse.ResponseUri;
                responseParameter.StatusCode = webResponse.StatusCode;
                using (StreamReader reader = new StreamReader(webResponse.GetResponseStream(), encoding))
                {
                    responseParameter.Body = reader.ReadToEnd();
                }
            }
            return responseParameter;
        }
        void SetHand(HttpWebRequest webRequest) 
        {
            webRequest.ContentType = "application/json";
            webRequest.Accept = "application/json";
            webRequest.Headers.Add("JWT-AUD", JWTAUD);
        }
    }
}
