using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WechatBakTool.Helpers;
using WechatBakTool.Model;
using WechatBakTool.ViewModel;

namespace WechatBakTool
{
    public class WXWorkspace
    {
        private UserBakConfig UserBakConfig = new UserBakConfig();
        public WXWorkspace(string path,string account = "") {
            string checkResult = Init(path, false, account);
            if (checkResult != "")
                new Exception(checkResult);
        }

        public WXWorkspace(UserBakConfig userBakConfig)
        {
            UserBakConfig = userBakConfig;
        }

        public void DecryptDB(string pid,int type,CreateWorkViewModel viewModel,string pwd = "")
        {
            if (UserBakConfig == null)
            {
                throw new Exception("没有工作区文件，无法解密");
            }

            if (!UserBakConfig.Decrypt)
            {
                byte[]? key = null;
                viewModel.LabelStatus = "正在获取秘钥，需要1 - 10秒左右";
                if(pwd == "")
                    key = DecryptionHelper.GetWechatKey(pid, type, UserBakConfig.Account);
                else
                {
                    key = new byte[pwd.Length / 2];
                    for(int i = 0;i<pwd.Length / 2; i++)
                    {
                        key[i] = Convert.ToByte(pwd.Substring(i * 2, 2), 16);
                    }
                    
                }
#if DEBUG
                File.WriteAllText("key.log", BitConverter.ToString(key!));
#endif
                if (key == null)
                {
                    throw new Exception("获取到的密钥为空，获取失败");
                }

                string source = Path.Combine(UserBakConfig.UserWorkspacePath, "OriginalDB");
                string to = Path.Combine(UserBakConfig.UserWorkspacePath, "DecDB");

                DecryptionHelper.DecryUserData(key, source, to, viewModel);
                UserBakConfig.Decrypt = true;

                WXUserReader reader = new WXUserReader(UserBakConfig);
                int[] count = reader.GetWXCount();
                UserBakConfig.Friends_Number = count[0].ToString();
                UserBakConfig.Msg_Number = count[1].ToString();
                SaveConfig(UserBakConfig);
            }
        }

        public void MoveDB(CreateWorkViewModel viewModel)
        {
            string sourceBase = Path.Combine(UserBakConfig.UserResPath, "Msg");
            string sourceMulit = Path.Combine(UserBakConfig.UserResPath, "Msg/Multi");
            string[] files = Directory.GetFiles(sourceBase);
            foreach (string file in files)
            {
                FileInfo fileInfo = new FileInfo(file);
                if (fileInfo.Extension == ".db")
                {
                    viewModel.LabelStatus = "正在迁移" + fileInfo.Name;
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
                    viewModel.LabelStatus = "正在迁移" + fileInfo.Name;
                    string to_path = Path.Combine(UserBakConfig.UserWorkspacePath, "OriginalDB", fileInfo.Name);
                    File.Copy(file, to_path, true);
                }
            }
        }
        public UserBakConfig ReturnConfig()
        {
            return UserBakConfig;
        }
        public static void SaveConfig(UserBakConfig userBakConfig, bool manual = false)
        {
            if(userBakConfig.UserWorkspacePath != "")
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(userBakConfig.UserWorkspacePath);
                if(directoryInfo.Parent != null)
                {
                    string json_path = Path.Combine(directoryInfo.Parent.FullName, userBakConfig.Manual ? userBakConfig.Hash + ".json" : userBakConfig.UserName + ".json");
                    string json = JsonConvert.SerializeObject(userBakConfig);
                    File.WriteAllText(json_path, json);
                }
            }
        }

        public void ManualInit()
        {
            Init("", true, "");
        }
        private string Init(string path,bool manual = false,string account = "")
        {
            string curPath = AppDomain.CurrentDomain.BaseDirectory;
            if (!manual)
            {
                string md5 = GetMd5Hash(path);
                string[] paths = path.Split(new string[] { "/", "\\" }, StringSplitOptions.None);
                string username = paths[paths.Length - 1];
                UserBakConfig.UserResPath = path;
                UserBakConfig.UserWorkspacePath = Path.Combine(curPath, "workspace", md5);
                UserBakConfig.Hash = md5;
                UserBakConfig.UserName = username;
                UserBakConfig.Account = account;
            }

            UserBakConfig.Manual = manual;

            if (!Directory.Exists(UserBakConfig.UserResPath) && !manual)
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
            SaveConfig(UserBakConfig, manual);
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
