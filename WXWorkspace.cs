using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    public class WXWorkspace
    {
        private UserBakConfig UserBakConfig = new UserBakConfig();
        public WXWorkspace(string path) {
            string checkResult = Init(path);
            if (checkResult != "")
                new Exception(checkResult);
        }

        public WXWorkspace(UserBakConfig userBakConfig)
        {
            UserBakConfig = userBakConfig;
        }
        public void MoveDB()
        {
            string sourceBase = Path.Combine(UserBakConfig.UserResPath, "Msg");
            string sourceMulit = Path.Combine(UserBakConfig.UserResPath, "Msg/Multi");
            string[] files = Directory.GetFiles(sourceBase);
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if(fileInfo.Extension == ".db")
                {
                    string to_path = Path.Combine(UserBakConfig.UserWorkspacePath, "OriginalDB", fileInfo.Name);
                    File.Copy(file, to_path, true);
                }
            }

            files = Directory.GetFiles(sourceMulit);
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Extension == ".db")
                {
                    string to_path = Path.Combine(UserBakConfig.UserWorkspacePath, "OriginalDB", fileInfo.Name);
                    File.Copy(file, to_path, true);
                }
            }
        }

        public static void SaveConfig(UserBakConfig userBakConfig)
        {
            if(userBakConfig.UserWorkspacePath != "")
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(userBakConfig.UserWorkspacePath);
                if(directoryInfo.Parent != null)
                {
                    string json_path = Path.Combine(directoryInfo.Parent.FullName, userBakConfig.UserName + ".json");
                    string json = JsonConvert.SerializeObject(userBakConfig);
                    File.WriteAllText(json_path, json);
                }
            }
        }
        private string Init(string path)
        {
            string curPath = AppDomain.CurrentDomain.BaseDirectory;
            string md5 = GetMd5Hash(path);
            string[] paths = path.Split(new string[] { "/", "\\" }, StringSplitOptions.None);
            string username = paths[paths.Length - 1];
            UserBakConfig.UserResPath = path;
            UserBakConfig.UserWorkspacePath = Path.Combine(curPath, "workspace", md5);
            UserBakConfig.Hash = md5;
            UserBakConfig.UserName = username;

            if (!Directory.Exists(UserBakConfig.UserResPath))
            {
                return "用户资源文件夹不存在，如需使用离线数据，请从工作区读取";
            }

            if (!Directory.Exists(UserBakConfig.UserWorkspacePath))
            {
                Directory.CreateDirectory(UserBakConfig.UserWorkspacePath);
            }

            string db = Path.Combine(UserBakConfig.UserWorkspacePath, "OriginalDB");
            string decDb = Path.Combine(UserBakConfig.UserWorkspacePath, "DecDB");
            if (!Directory.Exists(db))
            {
                Directory.CreateDirectory (db);
            }
            if (!Directory.Exists(decDb))
            {
                Directory.CreateDirectory(decDb);
            }
            SaveConfig(UserBakConfig);
            return "";
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
