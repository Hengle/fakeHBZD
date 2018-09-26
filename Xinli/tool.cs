using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System.Security.Cryptography;
using System.Net;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace Xinli
{
    class tool
    {
        public static void log(string str)
        {
            MainWindow.inFunction(str);
        }

        public static string getIP()
        {
            string location = null;
            //一般方法
            HttpWebRequest webclient =
                (HttpWebRequest)WebRequest.Create("http://100.64.0.1/");
            webclient.AllowAutoRedirect = false;
            HttpWebResponse resopnse = (HttpWebResponse)webclient.GetResponse();
            location = resopnse.Headers["Location"];
            return location.Substring(location.IndexOf("=") + 1, location.IndexOf("&") - location.IndexOf("=") - 1);
        }

        #region 读/写配置部分
        /// <summary>
        /// 修改AppSettings中配置
        /// </summary>
        /// <param name="key">key值</param>
        /// <param name="value">相应值</param>
        public static bool SetConfigValue(string key, string value)
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings[key] != null)
                    config.AppSettings.Settings[key].Value = value;
                else
                    config.AppSettings.Settings.Add(key, value);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取AppSettings中某一节点值
        /// </summary>
        /// <param name="key"></param>
        public static string GetConfigValue(string key)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            if (config.AppSettings.Settings[key] != null)
                return config.AppSettings.Settings[key].Value;
            else

                return string.Empty;
        }
        #endregion

        public static string getunix()
        {
            System.DateTime time = System.DateTime.Now;
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1, 0, 0, 0, 0));
            long t = (time.Ticks - startTime.Ticks) / 1000000;
            return t.ToString();
        }
    }

    class main
    {
        private const string testkey = "1234567890123456";

        private const string Host = "http://58.53.196.165:8080/";

        private static string publickey = null;

        private static string AccessToken = null;

        public static string username;
        public static string password;
        public static string remoteip;

        public static string getISPCode()
        {
            //network.clearCookie();
            string tmp = network.GhttpReq("http://58.53.199.78:8020/public_key?uid=0");
            JObject job = new JObject(JObject.Parse(tmp));
            if (job["content"].ToString() == "success")
            {
                publickey = job["pub"].ToString();
                return HBZD_Login();
            }
            else
                throw new MyException("error: " + job["content"].ToString());
        }

        public static string HBZD_Login()
        {
            string user = "12345678901";
            
            string encSession = Encrypt.GetRsaEncode(publickey, "123456");
            encSession = Uri.EscapeDataString(encSession);
            string tmp = network.PhttpReq("http://58.53.199.78:8020/login_validate", "username=" + user + "&password=" + encSession);
            JObject job = new JObject(JObject.Parse(tmp));
            if (job["content"].ToString() == "登陆成功")
            {
                tool.log("universityName: " + job["entity"]["universityName"].ToString());
                tool.log("classLevel: " + job["entity"]["classLevel"].ToString());

                string sessionid = tool.getunix();
                byte[] buffer = Encrypt.AesEncrypt(user + "$$" + sessionid, testkey);
                string entryptData = Uri.EscapeDataString(System.Convert.ToBase64String(buffer));

                tmp = network.GhttpReq("http://58.53.196.166:8080/consent?KeyId=1&sessionid=" + sessionid + "&entryptData=" + entryptData + "&type=checkAuto&sim=&userName=" + user);
                job = new JObject(JObject.Parse(tmp));
                string callback = Encrypt.AESDecrypt(job["entryptData"].ToString(), testkey);
                string password = callback.Substring(callback.IndexOf("$") + 1, callback.Length - callback.IndexOf("$") - 1);
                tool.SetConfigValue("ISPCode", password);
                return password;
            }
            else
            {
                throw new MyException(job["content"].ToString());
            }
        }

        public static void connect()
        {
            loginPortalGetKey();
            authenticate();
        }

        private static void loginPortalGetKey()
        {
            AccessToken = network.GhttpReq(Host + "wf.do?code=1&clientType=android&clientip=" + remoteip + "&device=Phone%3ARedmi+Note+4%5CSDK%3A25&version=8.1.0");
        }

        private static void authenticate()
        {
            string postData = "password=" + Encrypt.getPasswordEnc(password)
                   + "&code=8&clientType=android&clientip=" + remoteip + "&key=" + Encrypt.getSessionEnc(AccessToken) + "&username=" + username;
            string tmp = network.PhttpReq(Host + "wf.do", postData);
            if (tmp.IndexOf("auth00") >= 0)
                tool.log("连接成功: " + tmp);
            else throw new MyException("连接失败，返回信息: " + tmp);
        }
    }

    class network
    {
        static CookieContainer cookies = new CookieContainer();

        public static string GhttpReq(string url)
        {
            HttpWebRequest hRequest = (HttpWebRequest)HttpWebRequest.Create(url);

            hRequest.UserAgent = "Mozilla/Android/8.1.0/Redmi Note 3";
            hRequest.Headers.Add("App", "HBZD");

            hRequest.Method = "GET";

            hRequest.CookieContainer = cookies;
            //if (cookie != null)
            //    hRequest.Headers.Add("Cookie", cookie);

            hRequest.Timeout = 5 * 1000;

            HttpWebResponse response = (HttpWebResponse)hRequest.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            if (cookies != null)
            cookies = hRequest.CookieContainer;
            //if (response.Headers["Set-Cookie"] != null)
            //    cookie = response.Headers["Set-Cookie"];
            //cookie = response.Headers["Set-Cookie"];//.Substring(0, response.Headers["Set-Cookie"].IndexOf(";"))
            return retString;
        }

        public static string PhttpReq(string url, string postDataStr)
        {

            HttpWebRequest hRequest = (HttpWebRequest)HttpWebRequest.Create(url);
            hRequest.CookieContainer = new CookieContainer();
            
            hRequest.Headers.Add("App", "HBZD");
            hRequest.UserAgent = "Mozilla/Android/8.1.0/Redmi Note 3";
            hRequest.Headers.Add("Charset", "UTF-8");
            hRequest.ServicePoint.Expect100Continue=false;
            hRequest.KeepAlive = true;
            hRequest.Method = "POST";
            
            hRequest.ContentType = "application/x-www-form-urlencoded";

            hRequest.CookieContainer = cookies;

            hRequest.ContentLength = postDataStr.Length;

            byte[] dataParsed = Encoding.UTF8.GetBytes(postDataStr);
            hRequest.GetRequestStream().Write(dataParsed, 0, dataParsed.Length);


            hRequest.Timeout = 5 * 1000;

            HttpWebResponse response = (HttpWebResponse)hRequest.GetResponse();
            
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.UTF8);
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            if (cookies != null)
            cookies = hRequest.CookieContainer;
            //if (response.Headers["Set-Cookie"] != null)
            //    cookie = response.Headers["Set-Cookie"];
            return retString;
        }
    }

    class Encrypt
    {
        private const string AES_KEY_PASSWORD = "pass012345678910",
            AES_KEY_SESSION = "jyangzi5@163.com";

        public static byte[] AesEncrypt(string str, string key)
        {
            MemoryStream mStream = new MemoryStream();
            RijndaelManaged aes = new RijndaelManaged();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.PKCS7;
            aes.KeySize = 128;

            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            aes.Key = keyArray;

            CryptoStream cryptoStream = new CryptoStream(mStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
            try
            {
                byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(str);
                cryptoStream.Write(toEncryptArray, 0, toEncryptArray.Length);
                cryptoStream.FlushFinalBlock();

                byte[] buffer = mStream.ToArray();
                return buffer;
            }
            finally
            {
                cryptoStream.Close();
                mStream.Close();
                aes.Clear();
            }
        }

        public static string AESDecrypt(string str, string key)
        {
            byte[] keyArray = UTF8Encoding.UTF8.GetBytes(key);
            byte[] toEncryptArray = Convert.FromBase64String(str);

            RijndaelManaged rDel = new RijndaelManaged();
            rDel.Key = keyArray;
            rDel.Mode = CipherMode.ECB;
            rDel.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = rDel.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return UTF8Encoding.UTF8.GetString(resultArray);
        }

        public static string getSessionEnc(string session)
        {
            byte[] buffer = AesEncrypt(session, AES_KEY_SESSION);
            return BitConverter.ToString(buffer).Replace("-", "");
        }

        public static string getPasswordEnc(string sess)
        {
            byte[] buffer = Encrypt.AesEncrypt(sess, AES_KEY_PASSWORD);
            return BitConverter.ToString(buffer).Replace("-", "");
        }

        public static string GetRsaEncode(string publicKeystr, string text)
        {
            var publicKey = RSAPublicKeyJava2DotNet(publicKeystr);
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            var cipherbytes = rsa.Encrypt(Encoding.UTF8.GetBytes(text), false);
            return Convert.ToBase64String(cipherbytes);
        }

        private static string RSAPublicKeyJava2DotNet(string publicKey)
        {
            RsaKeyParameters publicKeyParam = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>",
                Convert.ToBase64String(publicKeyParam.Modulus.ToByteArrayUnsigned()),
                Convert.ToBase64String(publicKeyParam.Exponent.ToByteArrayUnsigned()));
        }
    }
}
