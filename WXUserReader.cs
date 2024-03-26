using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4;
using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using WechatBakTool.Helpers;
using WechatBakTool.Model;
using System.Windows;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading;
using Newtonsoft.Json;
using WechatBakTool.ViewModel;

namespace WechatBakTool
{
    public class WXUserReader
    {
        private Dictionary<string, SQLiteConnection> DBInfo = new Dictionary<string, SQLiteConnection>();
        private UserBakConfig? UserBakConfig = null;
        private Hashtable HeadImgCache = new Hashtable();
        private Hashtable UserNameCache = new Hashtable();
        private Hashtable EmojiCache = new Hashtable();
        private HttpClient httpClient = new HttpClient();
        public WXUserReader(UserBakConfig userBakConfig) {
            string path = Path.Combine(userBakConfig.UserWorkspacePath, "DecDB");
            UserBakConfig = userBakConfig;
            LoadDB(path);
            InitCache();
            EmojiCacheInit();
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

        private SQLiteConnection? getCon(string name)
        {
            if (DBInfo.ContainsKey(name))
            {
                return DBInfo[name];
            }
            else
            {
                return null;
            }
        }

        public void InitCache()
        {
            SQLiteConnection? con = getCon("Misc");
            if (con == null)
                return;

            string query = @"SELECT * FROM ContactHeadImg1";
            List<ContactHeadImg> imgs = con.Query<ContactHeadImg>(query);
            foreach(ContactHeadImg item in imgs)
            {
                if (!HeadImgCache.ContainsKey(item.usrName))
                {
                    HeadImgCache.Add(item.usrName, item);
                }
            }

            List<WXContact> contacts = GetWXContacts(null, true).ToList();
            foreach(WXContact contact in contacts)
            {
                if (!UserNameCache.ContainsKey(contact.UserName))
                    UserNameCache.Add(contact.UserName, contact);
            }
        }

        public void EmojiCacheInit()
        {
            string emoji_path = Path.Combine(UserBakConfig!.UserWorkspacePath, "Emoji");
            if (Directory.Exists(emoji_path))
            {
                string[] files = Directory.GetFiles(emoji_path);
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);
                    string[] names = fileInfo.Name.Split(".");
                    if (!EmojiCache.ContainsKey(names[0]))
                    {
                        EmojiCache.Add(names[0], 1);
                    }
                }
            }
        }

        public void PreDownloadEmoji(string username = "")
        {
            if (UserBakConfig == null)
                return;

            HttpClientHandler handler = new HttpClientHandler() { AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate };
            HttpClient httpClient = new HttpClient(handler);

            List<WXMsg> msgs = GetTypeMsg("47", username);
            int i = 0;

            // 下载前的Emoji Cache不用做了，在Init的时候已经做了
            foreach (var msg in msgs)
            {
                i++;
                if (i % 5 == 0)
                {
                    // 每5次让下载线程休息1秒
                    Thread.Sleep(1000);
                }
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(msg.StrContent);
                    XmlNode? node = xmlDocument.SelectSingleNode("/msg/emoji");
                    if (node != null)
                    {
                        if (node.Attributes != null)
                        {
                            string type = "";
                            string md5 = "";
                            string url = "";
                            XmlNode? item = node.Attributes.GetNamedItem("type");
                            type = item != null ? item.InnerText : "";

                            item = node.Attributes.GetNamedItem("md5");
                            md5 = item != null ? item.InnerText : "";

                            item = node.Attributes.GetNamedItem("cdnurl");
                            url = item != null ? item.InnerText : "";

                            if (EmojiCache.ContainsKey(md5))
                            {
                                i--;
                                continue;
                            }
                            if (url == "")
                            {
                                i--;
                                continue;
                            }
                            else
                            {
                                string path = Path.Combine(UserBakConfig.UserWorkspacePath, msg.StrTalker, "Emoji", md5 + ".gif");
                                try
                                {
                                    HttpResponseMessage res = httpClient.GetAsync(url).Result;
                                    if (res.IsSuccessStatusCode)
                                    {
                                        using (FileStream fs = File.Create(path))
                                        {
                                            res.Content.ReadAsStream().CopyTo(fs);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {

                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }

            // 下载完成后可能变化，检查一下
            EmojiCacheInit();
        }

        public byte[]? GetHeadImgCahce(string username)
        {
            if (HeadImgCache.ContainsKey(username))
            {
                ContactHeadImg? img = HeadImgCache[username] as ContactHeadImg;
                if (img == null)
                    return null;
                else
                    return img.smallHeadBuf;
            }
            return null;
        }

        public int[] GetWXCount()
        {
            SQLiteConnection? con = getCon("MicroMsg");
            if (con == null)
                return new int[] { 0, 0 };

            string query = @"select count(*) as count from contact where type != 4";
            int userCount = con.Query<WXCount>(query)[0].Count;

            int msgCount = 0;
            for (int i = 0; i <= 99; i++)
            {
                con = getCon("MSG" + i.ToString());
                if (con == null)
                    return new int[] { userCount, msgCount };

                query = "select count(*) as count from MSG";
                msgCount += con.Query<WXCount>(query)[0].Count;
            }
            return new int[] { userCount, msgCount };
        }

        public ObservableCollection<WXContact> GetWXContacts(string? name = null,bool all = false)
        {
            SQLiteConnection? con = getCon("MicroMsg");
            if (con == null)
                return new ObservableCollection<WXContact>();
            string query = @"select contact.*,session.strContent,contactHeadImgUrl.smallHeadImgUrl,contactHeadImgUrl.bigHeadImgUrl from contact 
            left join session on session.strUsrName = contact.username
            left join contactHeadImgUrl on contactHeadImgUrl.usrName = contact.username
            where type != 4 {searchName}
            order by nOrder desc";

            if (all)
            {
                query = query.Replace("where type != 4 ", "");
            }

            List<WXContact>? contacts = null;
            if (name != null)
            {
                query = query.Replace("{searchName}", " and (username like ? or alias like ? or nickname like ? or remark like ?)");
                contacts = con.Query<WXContact>(query, $"%{name}%", $"%{name}%", $"%{name}%", $"%{name}%");
            }
            else
            {
                query = query.Replace("{searchName}", "");
                contacts = con.Query<WXContact>(query);
            }

            foreach (WXContact contact in contacts)
            {
                if(contact.Remark != "")
                    contact.NickName = contact.Remark;

                byte[]? imgBytes = GetHeadImgCahce(contact.UserName);
                if (imgBytes != null)
                {
                    try
                    {
                        MemoryStream stream = new MemoryStream(imgBytes);
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();
                        bitmapImage.Freeze();
                        contact.Avatar = bitmapImage;
                    }
                    catch
                    {
#if DEBUG
                        File.AppendAllText("debug.log", string.Format("[D]{0} {1}:{2}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "BitmapConvert Err=>Length", imgBytes.Length));
#endif
                    }
                }
                else
                    continue;
            }

            return new ObservableCollection<WXContact>(contacts);
        }

        public List<WXUserImg>? GetUserImgs()
        {
            SQLiteConnection? con = getCon("MicroMsg");
            if (con == null)
                return null;
            string query = "select * from contactHeadImgUrl";
            return con.Query<WXUserImg>(query);
        }

        public List<WXChatRoom>? GetWXChatRooms()
        {
            SQLiteConnection? con = getCon("MicroMsg");
            if (con == null)
                return null;
            string query = "select * from ChatRoom";
            return con.Query<WXChatRoom>(query);
        }

        public List<WXMsg> GetTypeMsg(string type,string username)
        {
            List<WXMsg> tmp = new List<WXMsg>();
            for (int i = 0; i <= 99; i++)
            {
                SQLiteConnection? con = getCon("MSG" + i.ToString());
                if (con == null)
                    return tmp;

                List<WXMsg> wXMsgs;
                if (username == "")
                {
                    string query = "select * from MSG where Type=?";
                    wXMsgs = con.Query<WXMsg>(query, type);
                }
                else
                {
                    string query = "select * from MSG where Type=? and StrTalker = ?";
                    wXMsgs = con.Query<WXMsg>(query, type, username);
                }
                tmp.AddRange(wXMsgs);
            }
            return tmp;
        }

        public List<WXMsg>? GetWXMsgs(string uid,int time,int page)
        {
            List<WXMsg> tmp = new List<WXMsg>();
            for (int i = 0; i <= 99; i++)
            {
                SQLiteConnection? con = getCon("MSG" + i.ToString());
                if (con == null)
                    return tmp;

                List<WXMsg>? wXMsgs = null;
                string query = "select * from MSG where StrTalker=? and CreateTime>? Limit ?";
                wXMsgs = con.Query<WXMsg>(query, uid, time, page);
                if (wXMsgs.Count != 0) {
                    return ProcessMsg(wXMsgs, uid);
                }
            }
            return tmp;
        }
        public List<WXMsg>? GetWXMsgs(string uid,string msg = "")
        {
            List<WXMsg> tmp = new List<WXMsg>();
            for (int i = 0; i <= 99; i++)
            {
                SQLiteConnection? con = getCon("MSG" + i.ToString());
                if (con == null)
                    return tmp;

                List<WXMsg>? wXMsgs = null;
                if (msg == "")
                {
                    string query = "select * from MSG where StrTalker=?";
                    wXMsgs = con.Query<WXMsg>(query, uid);
                }
                else if (uid == "")
                {
                    string query = "select * from MSG where StrContent like ?";
                    wXMsgs = con.Query<WXMsg>(query, string.Format("%{0}%", msg));
                }
                else
                {
                    string query = "select * from MSG where StrTalker=? and StrContent like ?";
                    wXMsgs = con.Query<WXMsg>(query, uid, string.Format("%{0}%", msg));
                }

                tmp.AddRange(ProcessMsg(wXMsgs, uid));
            }
            return tmp;
        }

        public List<WXMsg>? GetWXMsgs(string uid, DatetimePickerViewModel dateModel)
        {
            List<WXMsg> tmp = new List<WXMsg>();
            for (int i = 0; i <= 99; i++)
            {
                SQLiteConnection? con = getCon("MSG" + i.ToString());
                if (con == null)
                    return tmp;

                List<WXMsg>? wXMsgs = null;
                string query = "";

                if (dateModel.DateType == 2 || dateModel.DateType == 3)
                {
                    query = "select * from MSG where StrTalker=? and date(createtime,'unixepoch') = ?";
                    wXMsgs = con.Query<WXMsg>(query, uid, dateModel.PickDate.ToString("yyyy-MM-dd"));
                }
                else if(dateModel.DateType == 4 )
                {
                    query = "select * from MSG where StrTalker=? and date(createtime,'unixepoch') >= ? and date(createtime,'unixepoch') <= ?";
                    wXMsgs = con.Query<WXMsg>(query, uid, dateModel.StartDate.ToString("yyyy-MM-dd"), dateModel.EndDate.ToString("yyyy-MM-dd"));
                }
                else
                {
                    query = "select * from MSG where StrTalker=?";
                    wXMsgs = con.Query<WXMsg>(query, uid);
                }
                
                tmp.AddRange(ProcessMsg(wXMsgs, uid));
            }
            return tmp;
        }

        private List<WXMsg> ProcessMsg(List<WXMsg> msgs,string uid)
        {
            foreach (WXMsg w in msgs)
            {
                if (UserNameCache.ContainsKey(w.StrTalker))
                {
                    WXContact? contact = UserNameCache[w.StrTalker] as WXContact;
                    if (contact != null)
                    {
                        if (contact.Remark != "")
                            w.NickName = contact.Remark;
                        else
                            w.NickName = contact.NickName;

                        w.StrTalker = contact.UserName;
                    }
                }
                else
                {
                    w.NickName = uid;
                }

                // 群聊处理
                if (uid.Contains("@chatroom"))
                {
                    string userId = "";

                    if (w.BytesExtra == null)
                        continue;

                    string sl = BitConverter.ToString(w.BytesExtra).Replace("-", "");

                    ProtoMsg protoMsg;
                    using (MemoryStream stream = new MemoryStream(w.BytesExtra))
                    {
                        protoMsg = ProtoBuf.Serializer.Deserialize<ProtoMsg>(stream);
                    }

                    if (protoMsg.TVMsg != null)
                    {
                        foreach (TVType _tmp in protoMsg.TVMsg)
                        {
                            if (_tmp.Type == 1)
                                userId = _tmp.TypeValue;
                        }
                    }


                    if (!w.IsSender)
                    {
                        if (UserNameCache.ContainsKey(userId))
                        {
                            WXContact? contact = UserNameCache[userId] as WXContact;
                            if (contact != null)
                                w.NickName = contact.Remark == "" ? contact.NickName : contact.Remark;
                        }
                        else
                        {
                            w.NickName = userId;
                        }
                    }
                }


                // 发送人名字处理
                if (w.IsSender)
                    w.NickName = "我";

                w.DisplayContent = w.StrContent;
                // 额外格式处理
                if (w.Type != 1)
                {
                    if (w.Type == 10000)
                    {
                        w.Type = 1;
                        w.NickName = "系统消息";
                        w.DisplayContent = w.StrContent.Replace("<revokemsg>", "").Replace("</revokemsg>", "");
                    }
                    else if (w.Type == 49 && (w.SubType == 6 || w.SubType == 19 || w.SubType == 40))
                    {
                        WXSessionAttachInfo? attachInfos = GetWXMsgAtc(w);
                        if (attachInfos == null)
                        {
                            w.DisplayContent = "附件不存在";
                        }
                        else
                        {
                            w.DisplayContent = Path.Combine(UserBakConfig!.UserResPath, attachInfos.attachPath);
                        }
                    }
                    else
                    {
                        w.DisplayContent = "[界面未支持格式]Type=" + w.Type;
                    }
                }
            }
            return msgs;
        }
        public List<WXSessionAttachInfo>? GetWXMsgAtc()
        {
            SQLiteConnection? con = getCon("MultiSearchChatMsg");
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
            SQLiteConnection? con = getCon("MultiSearchChatMsg");
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
                SQLiteConnection? con = getCon("MediaMSG" + i.ToString());
                if (con == null)
                    continue;

                string query = "select * from Media where Reserved0=?";
                List<WXMediaMsg> wXMsgs = con.Query<WXMediaMsg>(query, msg.MsgSvrID);
                if (wXMsgs.Count != 0)
                    return wXMsgs[0];
            }
            return null;
        }
        public string? GetAttachment(WXMsgType type, WXMsg msg)
        {
            if (UserBakConfig == null)
                return null;

            string? tmpPath = Path.Combine(UserBakConfig.UserWorkspacePath, msg.StrTalker, "Temp");
            if (!Directory.Exists(tmpPath))
                Directory.CreateDirectory(tmpPath);

            // 这部分是查找
            // 如果是图片和视频，从附件库中搜索
            string? path = null;
            if (type == WXMsgType.Image || type == WXMsgType.Video || type == WXMsgType.File)
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
            else if (type == WXMsgType.Emoji)
            {
                try
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.LoadXml(msg.StrContent);
                    XmlNode? node = xmlDocument.SelectSingleNode("/msg/emoji");
                    if (node != null)
                    {
                        if (node.Attributes != null)
                        {
                            XmlNode? item = node.Attributes.GetNamedItem("md5");
                            string md5 = item != null ? item.InnerText : "";
                            if (EmojiCache.ContainsKey(md5))
                            {
                                path = string.Format("Emoji\\{0}.gif", md5);
                            }
                        }
                    }
                }
                catch
                {
                    return null;
                }
                
            }

            if (path == null)
                return null;

            // 这部分是解密
            // 获取到原路径后，开始进行解密转移,只有图片和语音需要解密，解密后是直接归档目录
            if (type == WXMsgType.Image || type == WXMsgType.Audio)
            {
                path = DecryptAttachment(type, path, msg.StrTalker);
            }
            else if (type == WXMsgType.Video || type == WXMsgType.File)
            {
                string to_dir;
                if (type == WXMsgType.Video)
                    to_dir = Path.Combine(UserBakConfig.UserWorkspacePath, msg.StrTalker, "Video");
                else
                    to_dir = Path.Combine(UserBakConfig.UserWorkspacePath, msg.StrTalker, "File");
                if (!Directory.Exists(to_dir))
                    Directory.CreateDirectory(to_dir);
                FileInfo fileInfo = new FileInfo(path);
                // 目标视频路径
                string to_file_path = Path.Combine(to_dir, fileInfo.Name);
                // 视频的路径是相对路径，需要加上资源目录
                path = Path.Combine(UserBakConfig.UserResPath, path);
                // 原文件存在，目标不存在
                if (!File.Exists(to_file_path) && File.Exists(path))
                {
                    // 复制
                    File.Copy(path, to_file_path);
                    path = to_file_path;
                }
                else if (File.Exists(to_file_path))
                {
                    path = to_file_path;
                }
                else
                    return null;
            }

            if (path == null)
                return null;

            // 改相对路径
            path = path.Replace(UserBakConfig.UserWorkspacePath + "\\", "");
            return path;

        }
        public string? DecryptAttachment(WXMsgType type, string path,string username)
        {
            if (UserBakConfig == null)
                return null;

            string? file_path = null;
            switch (type)
            {
                case WXMsgType.Image:
                    string img_dir = Path.Combine(UserBakConfig.UserWorkspacePath, username, "Image");
                    if (!Directory.Exists(img_dir))
                        Directory.CreateDirectory(img_dir);
                    // 图片的路径是相对路径，需要加上资源目录
                    path = Path.Combine(UserBakConfig.UserResPath, path);
                    if (!File.Exists(path))
                        return null;
                    byte[] decFileByte = DecryptionHelper.DecImage(path);
                    if (decFileByte.Length < 2)
                        new Exception("解密失败，可能是未支持的格式");
                    string decFiletype = DecryptionHelper.CheckFileType(decFileByte);
                    file_path = DecryptionHelper.SaveDecImage(decFileByte, path, img_dir, decFiletype);
                    break;
                case WXMsgType.Audio:
                    string audio_dir = Path.Combine(UserBakConfig.UserWorkspacePath, username, "Audio");
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
                SQLiteConnection? con = getCon("MSG" + i.ToString());
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
            return g;
        }
    }

    public enum WXMsgType
    {
        Image = 0,
        Video = 1,
        Audio = 2,
        File = 3,
        Emoji = 4,
    }
}
