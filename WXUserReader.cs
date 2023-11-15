using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Xml.Linq;
using WechatPCMsgBakTool.Helpers;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    public class WXUserReader
    {
        private Dictionary<string, SQLiteConnection> DBInfo = new Dictionary<string, SQLiteConnection>();
        private UserBakConfig? UserBakConfig = null;
        public WXUserReader(UserBakConfig userBakConfig) {
            string path = Path.Combine(userBakConfig.UserWorkspacePath, "DecDB");
            UserBakConfig = userBakConfig;
            LoadDB(path);
        }

        public void LoadDB(string path)
        {
            string[] dbFileList = Directory.GetFiles(path);
            foreach (var item in dbFileList)
            {
                FileInfo fileInfo = new FileInfo(item);
                if (fileInfo.Extension != ".db")
                    continue;
                SQLiteConnection con = new SQLiteConnection(item);
                string dbName = fileInfo.Name.Split('.')[0];
                DBInfo.Add(dbName, con);
            }
        }

        public List<WXContact>? GetWXContacts(string? name = null)
        {
            SQLiteConnection con = DBInfo["MicroMsg"];
            if (con == null)
                return null;
            string query = "select * from contact";
            if (name != null)
            {
                query = "select * from contact where username like ? or alias like ? or nickname like ? or remark like ?";
                return con.Query<WXContact>(query, $"%{name}%", $"%{name}%", $"%{name}%", $"%{name}%");
            }
            return con.Query<WXContact>(query);
        }

        public List<WXMsg>? GetWXMsgs(string uid,string msg = "")
        {
            List<WXMsg> tmp = new List<WXMsg>();
            for (int i = 0; i <= 99; i++)
            {
                if(DBInfo.ContainsKey("MSG" + i.ToString()))
                {
                    SQLiteConnection con = DBInfo["MSG" + i.ToString()];
                    if (con == null)
                        return tmp;

                    List<WXMsg>? wXMsgs = null;
                    if (msg == "")
                    {
                        string query = "select * from MSG where StrTalker=?";
                        wXMsgs = con.Query<WXMsg>(query, uid);
                    }
                    else if(uid == "")
                    {
                        string query = "select * from MSG where StrContent like ?";
                        wXMsgs = con.Query<WXMsg>(query, string.Format("%{0}%", msg));
                    }
                    else
                    {
                        string query = "select * from MSG where StrTalker=? and StrContent like ?";
                        wXMsgs = con.Query<WXMsg>(query, uid, string.Format("%{0}%", msg));
                    }

                    foreach (WXMsg w in wXMsgs)
                    {
                        tmp.Add(w);
                    }
                }
            }
            return tmp;
        }
        public List<WXSessionAttachInfo>? GetWXMsgAtc()
        {
            SQLiteConnection con = DBInfo["MultiSearchChatMsg"];
            if (con == null)
                return null;

            string query = "select * from SessionAttachInfo";
            List<WXSessionAttachInfo> list = con.Query<WXSessionAttachInfo>(query);
            if (list.Count != 0)
            {
                return list;
            }
            else
                return null;
        }
        public WXSessionAttachInfo? GetWXMsgAtc(WXMsg msg)
        {
            SQLiteConnection con = DBInfo["MultiSearchChatMsg"];
            if (con == null)
                return null;

            string query = "select * from SessionAttachInfo where msgId = ? order by attachsize desc";
            List<WXSessionAttachInfo> list = con.Query<WXSessionAttachInfo>(query, msg.MsgSvrID);
            if (list.Count != 0)
            {
                //部分附件可能有md5校验，这里移除校验，给的是正确路径
                WXSessionAttachInfo acc = list[0];
                int index = acc.attachPath.IndexOf(".dat");
                int index2 = acc.attachPath.IndexOf(".dat");
                if (acc.attachPath.Length - index > 10 && index != -1)
                {
                    acc.attachPath = acc.attachPath.Substring(0, acc.attachPath.Length - 32);
                }
                if (acc.attachPath.Length - index2 > 10 && index2 != -1)
                {
                    acc.attachPath = acc.attachPath.Substring(0, acc.attachPath.Length - 32);
                }
                return acc;
            }
            else
                return null;
        }
        public WXMediaMsg? GetVoiceMsg(WXMsg msg)
        {
            for (int i = 0; i <= 99; i++)
            {
                if(DBInfo.ContainsKey("MediaMSG" + i.ToString()))
                {
                    SQLiteConnection con = DBInfo["MediaMSG" + i.ToString()];
                    if (con == null)
                        continue;

                    string query = "select * from Media where Reserved0=?";
                    List<WXMediaMsg> wXMsgs = con.Query<WXMediaMsg>(query, msg.MsgSvrID);
                    if (wXMsgs.Count != 0)
                        return wXMsgs[0];
                }
            }
            return null;
        }
        public string? GetAttachment(WXMsgType type, WXMsg msg)
        {
            if (UserBakConfig == null)
                return null;

            string? tmpPath = Path.Combine(UserBakConfig.UserWorkspacePath, "Temp");
            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            // 如果是图片和视频，从附件库中搜索
            string? path = null;
            if (type == WXMsgType.Image || type == WXMsgType.Video)
            {
                WXSessionAttachInfo? atcInfo = GetWXMsgAtc(msg);
                if (atcInfo == null)
                    return null;
                path = atcInfo.attachPath;
            }
            // 如果是从语音，从媒体库查找
            else if (type == WXMsgType.Audio)
            {
                WXMediaMsg? voiceMsg = GetVoiceMsg(msg);
                if (voiceMsg == null)
                    return null;
                if (voiceMsg.Buf == null)
                    return null;

                // 从DB取音频文件到临时目录
                string tmp_file_path = Path.Combine(tmpPath, voiceMsg.Key + ".arm");
                using (FileStream stream = new FileStream(tmp_file_path, FileMode.OpenOrCreate))
                {
                    stream.Write(voiceMsg.Buf, 0, voiceMsg.Buf.Length);
                }
                path = tmp_file_path;
            }

            if (path == null)
                return null;
            
            // 获取到原路径后，开始进行解密转移,只有图片和语音需要解密，解密后是直接归档目录
            if(type == WXMsgType.Image || type== WXMsgType.Audio)
            {
                path = DecryptAttachment(type, path);
            }
            else if (type == WXMsgType.Video)
            {
                string video_dir = Path.Combine(UserBakConfig.UserWorkspacePath, "Video");
                if(!Directory.Exists(video_dir))
                    Directory.CreateDirectory(video_dir);
                FileInfo fileInfo = new FileInfo(path);
                string video_file_path = Path.Combine(video_dir, fileInfo.Name);
                // 视频的路径是相对路径，需要加上资源目录
                path = Path.Combine(UserBakConfig.UserResPath, path);
                if(!File.Exists(video_file_path))
                    File.Copy(path, video_file_path);
                path = video_file_path;
            }

            if (path == null)
                return null;

            // 改相对路径
            path = path.Replace(UserBakConfig.UserWorkspacePath + "\\", "");
            return path;

        }
        public string? DecryptAttachment(WXMsgType type, string path)
        {
            if (UserBakConfig == null)
                return null;

            string? file_path = null;
            switch (type)
            {
                case WXMsgType.Image:
                    string img_dir = Path.Combine(UserBakConfig.UserWorkspacePath, "Image");
                    if (!Directory.Exists(img_dir))
                        Directory.CreateDirectory(img_dir);
                    // 图片的路径是相对路径，需要加上资源目录
                    path = Path.Combine(UserBakConfig.UserResPath, path);
                    byte[] decFileByte = DecryptionHelper.DecImage(path);
                    string decFiletype = DecryptionHelper.CheckFileType(decFileByte);
                    file_path = DecryptionHelper.SaveDecImage(decFileByte, path, img_dir, decFiletype);
                    break;
                case WXMsgType.Audio:
                    string audio_dir = Path.Combine(UserBakConfig.UserWorkspacePath, "Audio");
                    if (!Directory.Exists(audio_dir))
                        Directory.CreateDirectory(audio_dir);
                    FileInfo fileInfo = new FileInfo(path);
                    string audio_file_dir = Path.Combine(audio_dir, fileInfo.Name + ".mp3");
                    ToolsHelper.DecodeVoice(path, path + ".pcm", audio_file_dir);
                    file_path = audio_file_dir;
                    break;
            }
            return file_path;
        }
        public List<WXMsgGroup> GetWXMsgGroup()
        {
            List<WXMsgGroup> g = new List<WXMsgGroup>();
            for (int i = 0; i <= 99; i++)
            {
                if (DBInfo.ContainsKey("MSG" + i.ToString()))
                {
                    SQLiteConnection con = DBInfo["MSG" + i.ToString()];
                    if (con == null)
                        return g;

                    string query = "select StrTalker,Count(localId) as MsgCount from MSG GROUP BY StrTalker";
                    List<WXMsgGroup> wXMsgs = con.Query<WXMsgGroup>(query);
                    foreach (WXMsgGroup w in wXMsgs)
                    {
                        WXMsgGroup? tmp = g.Find(x => x.UserName == w.UserName);
                        if (tmp == null)
                            g.Add(w);
                        else
                            tmp.MsgCount += g.Count;
                    }
                }
            }
            return g;
        }
    }

    public enum WXMsgType
    {
        Image = 0,
        Video = 1,
        Audio = 2,
        File = 3,
    }
}
