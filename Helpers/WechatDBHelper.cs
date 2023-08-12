using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool.Helpers
{
    public class WechatDBHelper
    {
        private static string ResPath = "";
        private static string CurrentPath = AppDomain.CurrentDomain.BaseDirectory;
        private static string UserWorkPath = "";
        private static int MaxMediaDBCount = 0;
        private static int MaxMsgDBCount = 0;
        public static DBInfo GetDBInfo()
        {
            return new DBInfo() { MaxMediaDBCount = MaxMediaDBCount, MaxMsgDBCount = MaxMsgDBCount, UserPath = UserWorkPath, ResPath = ResPath };
        }

        public static DBInfo GetDBinfoOnLocal(string path)
        {
            string md5 = GetMd5Hash(path);
            string tmpPath = Path.Combine(CurrentPath, md5);

            string decPath = Path.Combine(tmpPath, "DecDB");
            string[] files = Directory.GetFiles(decPath);
            int media = 0;
            int msg = 0;
            foreach(string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if(fileInfo.Extension == ".db")
                {
                    string name = fileInfo.Name.Replace(".db", "");
                    if(name.Substring(0,3) == "MSG")
                    {
                        name = name.Replace("MSG", "");
                        int currentDB = int.Parse(name);
                        if(currentDB > msg)
                            msg = currentDB;
                        continue;
                    }
                    if(name.Substring(0,8)== "MediaMSG")
                    {
                        name = name.Replace("MediaMSG", "");
                        int currentDB = int.Parse(name);
                        if (currentDB > media)
                            media = currentDB;
                        continue;
                    }
                }
            }
            return new DBInfo() { MaxMediaDBCount = media, MaxMsgDBCount = msg, UserPath = tmpPath, ResPath = path };
        }

        public static void CreateUserWorkPath(string path)
        {
            ResPath = path;
            string md5 = GetMd5Hash(path);
            string tmpPath = Path.Combine(CurrentPath, md5);
            if (!Directory.Exists(tmpPath))
            {
                Directory.CreateDirectory(tmpPath);
            }
            UserWorkPath = tmpPath;
        }

        public static string MoveUserData(string path)
        {
            if(UserWorkPath != "")
            {
                //创建db库
                string db = Path.Combine(UserWorkPath, "DB");
                if (!Directory.Exists(db))
                {
                    Directory.CreateDirectory(db);
                }

                //核心数据库查找
                List<string> dbPathArray = new List<string>();

                string userDBPath = Path.Combine(path, "Msg");
                if (!Directory.Exists(userDBPath))
                    return "用户目录不存在，创建失败";

                string mainDB = Path.Combine(userDBPath, "MicroMsg.db");
                if (!File.Exists(mainDB))
                    return "微信主数据库不存在，创建失败";
                else
                    dbPathArray.Add(mainDB);

                string actDB = Path.Combine(userDBPath, "MultiSearchChatMsg.db");
                if(!File.Exists(actDB))
                    return "微信附件数据库不存在，创建失败";
                else
                    dbPathArray.Add(actDB);

                string dbmsg = Path.Combine(userDBPath, "Multi");
                bool mediaDBExists = false;
                bool msgDBExists = false;
                for(int i = 0; i < 100; i++)
                {
                    string mediaDBPath = Path.Combine(dbmsg, string.Format("MediaMSG{0}.db", i.ToString()));
                    string msgDBPath = Path.Combine(dbmsg, string.Format("MSG{0}.db", i.ToString()));

                    mediaDBExists = File.Exists(mediaDBPath);
                    msgDBExists = File.Exists(msgDBPath);

                    if (i == 0 && !mediaDBExists && !msgDBExists)
                    {
                        return "微信聊天记录数据不存在，创建失败";
                    }

                    if(mediaDBExists)
                        dbPathArray.Add(mediaDBPath);

                    if (msgDBExists)
                        dbPathArray.Add(msgDBPath);

                    if (!msgDBExists && !msgDBExists)
                        break;
                }

                foreach(string dbPath in dbPathArray) { 
                    FileInfo file = new FileInfo(dbPath);
                    string to = Path.Combine(db, file.Name);
                    if(!File.Exists(to))
                        File.Copy(dbPath, to);
                }
                return "";

            }
            return "请复制目录至文本框内";
        }
        public static void DecryUserData(byte[] key)
        {
            string dbPath = Path.Combine(UserWorkPath, "DB");
            string decPath = Path.Combine(UserWorkPath, "DecDB");
            if(!Directory.Exists(decPath))
                Directory.CreateDirectory(decPath);

            string[] filePath = Directory.GetFiles(dbPath);
            foreach (string file in filePath)
            {
                FileInfo info = new FileInfo(file);
                var db_bytes = File.ReadAllBytes(file);
                var decrypted_file_bytes = DecryptionHelper.DecryptDB(db_bytes, key);
                if (decrypted_file_bytes == null || decrypted_file_bytes.Length == 0)
                {
                    Console.WriteLine("解密后的数组为空");
                }
                else
                {
                    File.WriteAllBytes(Path.Combine(decPath, info.Name), decrypted_file_bytes);
                }
            }
        }
        private static string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

                // Create a new Stringbuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data 
                // and format each one as a hexadecimal string.
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}
