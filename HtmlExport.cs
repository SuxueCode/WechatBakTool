using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatPCMsgBakTool.Interface;
using WechatPCMsgBakTool.Model;
using System.Xml;

namespace WechatPCMsgBakTool
{
    public class HtmlExport : IExport
    {
        private string HtmlBody = "";
        private WXSession? Session = null;
        public void InitTemplate(WXSession session)
        {
            Session = session;
            HtmlBody = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>溯雪微信聊天记录备份工具</title><style>p{margin:0px;}.msg{padding-bottom:10px;}.nickname{font-size:10px;}.content{font-size:14px;}</style></head><body>";

            HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\"><b>与 {0}({1}) 的聊天记录</b></p>", Session.NickName, Session.UserName);
            HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\"><b>导出时间：{0}</b></p><hr/>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        public void InitTemplate(WXContact contact)
        {
            WXSession session = new WXSession();
            session.NickName = contact.NickName;
            session.UserName = contact.UserName;
            InitTemplate(session);
        }

        public void Save(string path = "",bool append = false)
        {
            if (!append)
            {
                File.WriteAllText(path, HtmlBody);
            }
            else
            {
                File.AppendAllText(path, HtmlBody);
                HtmlBody = "";
            }
        }

        public void SetEnd()
        {
            HtmlBody += "</body></html>";
        }

        public void SetMsg(WXUserReader reader,WXContact contact)
        {
            if (Session == null)
                throw new Exception("请初始化模版：Not Use InitTemplate");

            List<WXMsg>? msgList = reader.GetWXMsgs(contact.UserName);
            if (msgList == null)
                throw new Exception("获取消息失败，请确认数据库读取正常");

            msgList.Sort((x, y) => x.CreateTime.CompareTo(y.CreateTime));

            foreach (var msg in msgList)
            {
                HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\">{0} <span style=\"padding-left:10px;\">{1}</span></p>", msg.IsSender ? "我" : Session.NickName, TimeStampToDateTime(msg.CreateTime).ToString("yyyy-MM-dd HH:mm:ss"));

                if (msg.Type == 1)
                    HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", msg.StrContent);
                else if (msg.Type == 3)
                {
                    string? path = reader.GetAttachment(WXMsgType.Image, msg);
                    if (path == null)
                    {
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "图片转换出现错误或文件不存在");
                        continue;
                    }
                    HtmlBody += string.Format("<p class=\"content\"><img src=\"{0}\" style=\"max-height:1000px;max-width:1000px;\"/></p></div>", path);
                }
                else if (msg.Type == 43)
                {
                    string? path = reader.GetAttachment(WXMsgType.Video, msg);
                    if (path == null)
                    {
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "视频不存在");
                        continue;
                    }
                    HtmlBody += string.Format("<p class=\"content\"><video controls style=\"max-height:300px;max-width:300px;\"><source src=\"{0}\" type=\"video/mp4\" /></video></p></div>", path);
                }
                else if(msg.Type== 49)
                {
                    using (var decoder = LZ4Decoder.Create(true, 64))
                    {
                        byte[] target = new byte[10240];
                        int res = 0;
                        if(msg.CompressContent != null)
                            res = LZ4Codec.Decode(msg.CompressContent, 0, msg.CompressContent.Length, target, 0, target.Length);

                        byte[] data = target.Skip(0).Take(res).ToArray();
                        string xml = Encoding.UTF8.GetString(data);
                        if (!string.IsNullOrEmpty(xml))
                        {
                            xml = xml.Replace("\n", "");
                            XmlDocument xmlObj = new XmlDocument();
                            xmlObj.LoadXml(xml);
                            if(xmlObj.DocumentElement != null)
                            {
                                string title = "";
                                string appName = "";
                                string url = "";
                                XmlNodeList? findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/title");
                                if(findNode != null)
                                {
                                    if(findNode.Count > 0)
                                    {
                                        title = findNode[0]!.InnerText;
                                    }
                                }
                                findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/sourcedisplayname");
                                if (findNode != null)
                                {
                                    if (findNode.Count > 0)
                                    {
                                        appName = findNode[0]!.InnerText;
                                    }
                                }
                                findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/url");
                                if (findNode != null)
                                {
                                    if (findNode.Count > 0)
                                    {
                                        url = findNode[0]!.InnerText;
                                    }
                                }
                                HtmlBody += string.Format("<p class=\"content\">{0}|{1}</p><p><a href=\"{2}\">点击访问</a></p></div>", appName, title, url);

                            }
                        }

                    }
                }
                else if (msg.Type == 34)
                {
                    string? path = reader.GetAttachment(WXMsgType.Audio, msg);
                    if (path == null)
                    {
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "语音不存在");
                        continue;
                    }
                    HtmlBody += string.Format("<p class=\"content\"><audio controls src=\"{0}\"></audio></p></div>", path);
                }
                else
                {
                    HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "暂未支持的消息");
                }
            }

        }
        private static DateTime TimeStampToDateTime(long timeStamp, bool inMilli = false)
        {
            DateTimeOffset dateTimeOffset = inMilli ? DateTimeOffset.FromUnixTimeMilliseconds(timeStamp) : DateTimeOffset.FromUnixTimeSeconds(timeStamp);
            return dateTimeOffset.LocalDateTime;
        }
    }
}
