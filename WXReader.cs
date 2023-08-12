using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Interop;
using WechatPCMsgBakTool.Helpers;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    public class WXReader
    {
        private DBInfo DecDBInfo;
        private Dictionary<string, SQLiteConnection> DBInfo = new Dictionary<string, SQLiteConnection>();
        public WXReader(DBInfo? info = null) {
            if (info == null)
                DecDBInfo = WechatDBHelper.GetDBInfo();
            else
                DecDBInfo = info;

            string[] dbFileList = Directory.GetFiles(Path.Combine(DecDBInfo.UserPath, "DecDB"));
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

        public List<WXSession>? GetWXSessions(string? name = null)
        {
            SQLiteConnection con = DBInfo["MicroMsg"];
            if (con == null)
                return null;
            string query = "select * from session";
            if(name != null)
            {
                query = "select * from session where strUsrName = ?";
                return con.Query<WXSession>(query, name);
            }
            return con.Query<WXSession>(query);
        }

        public List<WXContact>? GetUser(string? name = null)
        {
            SQLiteConnection con = DBInfo["MicroMsg"];
            if (con == null)
                return null;
            string query = "select * from contact";
            if (name != null)
            {
                query = "select * from contact where username = ? or alias = ?";
                return con.Query<WXContact>(query, name, name);
            }
            return con.Query<WXContact>(query);
        }

        public WXSessionAttachInfo? GetWXMsgAtc(WXMsg msg)
        {
            SQLiteConnection con = DBInfo["MultiSearchChatMsg"];
            if (con == null)
                return null;

            string query = "select * from SessionAttachInfo where msgId = ? order by attachsize desc";
            List<WXSessionAttachInfo> list = con.Query<WXSessionAttachInfo>(query, msg.MsgSvrID);
            if (list.Count != 0)
                return list[0];
            else
                return null;
        }

        public List<WXMsg> GetMsgs(string uid)
        {
            List<WXMsg> tmp = new List<WXMsg>();
            for(int i = 0; i <= DecDBInfo.MaxMsgDBCount; i++)
            {
                SQLiteConnection con = DBInfo["MSG" + i.ToString()];
                if (con == null)
                    continue;

                string query = "select * from MSG where StrTalker=?";
                List<WXMsg> wXMsgs = con.Query<WXMsg>(query, uid);
                foreach(WXMsg w in wXMsgs)
                {
                    tmp.Add(w);
                }
            }
            return tmp;
        }

        public string? GetVideo(WXMsg msg)
        {
            WXSessionAttachInfo? attachInfo = GetWXMsgAtc(msg);
            if (attachInfo == null)
                return null;

            string resBasePath = Path.Combine(DecDBInfo.ResPath, attachInfo.attachPath);
            if (!File.Exists(resBasePath))
                return null;

            string videoPath = Path.Combine(DecDBInfo.UserPath, msg.StrTalker, "Video");
            if (!Directory.Exists(videoPath))
                Directory.CreateDirectory(videoPath);

            FileInfo fileInfo = new FileInfo(resBasePath);
            string savePath = Path.Combine(videoPath, fileInfo.Name);

            if(!File.Exists(savePath))
                File.Copy(resBasePath, savePath, false);
            return savePath;
        }

        public string GetSavePath(WXSession session)
        {
            string savePath = Path.Combine(DecDBInfo.UserPath, session.UserName + ".html");
            return savePath;
        }

        public string? GetImage(WXMsg msg)
        {
            WXSessionAttachInfo? attachInfo = GetWXMsgAtc(msg);
            if (attachInfo == null)
                return null;

            string resBasePath = Path.Combine(DecDBInfo.ResPath, attachInfo.attachPath);

            //部分attachpath可能会附加md5校验，这里做处理
            int index = attachInfo.attachPath.IndexOf(".dat");
            if (attachInfo.attachPath.Length - index > 10)
            {
                resBasePath = resBasePath.Substring(0, resBasePath.Length - 32);
            }

            if (!File.Exists(resBasePath))
                return null;

            string imgPath = Path.Combine(DecDBInfo.UserPath, msg.StrTalker, "Image");
            if (!Directory.Exists(imgPath))
                Directory.CreateDirectory(imgPath);

            string img = DecImage(resBasePath, imgPath);
            return img;
        }

        private string DecImage(string source,string toPath)
        {
            //读取数据
            byte[] fileBytes = File.ReadAllBytes(source);
            //算差异转换
            byte key = getImgKey(fileBytes);
            fileBytes = ConvertData(fileBytes, key);
            //取文件类型
            string type = CheckFileType(fileBytes);
            //
            FileInfo fileInfo = new FileInfo(source);
            string fileName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4);
            string saveFilePath = Path.Combine(toPath, fileName + type);
            using (FileStream fileStream = File.OpenWrite(saveFilePath))
            {
                fileStream.Write(fileBytes, 0, fileBytes.Length);
                fileStream.Flush();
            }
            return saveFilePath;
        }
        private string CheckFileType(byte[] data)
        {
            switch (data[0])
            {
                case 0XFF:  //byte[] jpg = new byte[] { 0xFF, 0xD8, 0xFF };
                    {
                        if (data[1] == 0xD8 && data[2] == 0xFF)
                        {
                            return ".jpg";
                        }
                        break;
                    }
                case 0x89:  //byte[] png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
                    {
                        if (data[1] == 0x50 && data[2] == 0x4E && data[7] == 0x0A)
                        {
                            return ".png";
                        }
                        break;
                    }
                case 0x42:  //byte[] bmp = new byte[] { 0x42, 0x4D };
                    {
                        if (data[1] == 0X4D)
                        {
                            return ".bmp";
                        }
                        break;
                    }
                case 0x47:  //byte[] gif = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39(0x37), 0x61 };
                    {
                        if (data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38 && data[5] == 0x61)
                        {
                            return ".gif";
                        }
                        break;
                    }
                case 0x49:  // byte[] tif = new byte[] { 0x49, 0x49, 0x2A, 0x00 };
                    {
                        if (data[1] == 0x49 && data[2] == 0x2A && data[3] == 0x00)
                        {
                            return ".tif";
                        }
                        break;
                    }
                case 0x4D:  //byte[] tif = new byte[] { 0x4D, 0x4D, 0x2A, 0x00 };
                    {
                        if (data[1] == 0x4D && data[2] == 0x2A && data[3] == 0x00)
                        {
                            return ".tif";
                        }
                        break;
                    }
            }

            return ".dat";
        }
        private byte getImgKey(byte[] fileRaw)
        {
            byte[] raw = new byte[8];
            for (int i = 0; i < 8; i++)
            {
                raw[i] = fileRaw[i];
            }

            for (byte key = 0x01; key < 0xFF; key++)
            {
                byte[] buf = new byte[8];
                raw.CopyTo(buf, 0);

                if (CheckFileType(ConvertData(buf, key)) != ".dat")
                {
                    return key;
                }
            }
            return 0x00;
        }
        private byte[] ConvertData(byte[] data, byte key)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] ^= key;
            }

            return data;
        }
    }
}
