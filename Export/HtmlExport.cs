using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatBakTool.Model;
using System.Xml;
using Newtonsoft.Json;
using WechatBakTool.ViewModel;
using System.Security.Policy;
using System.Windows;
using System.Xml.Linq;
using WechatBakTool.Helpers;

namespace WechatBakTool.Export
{
    public class HtmlExport : IExport
    {
        private string HtmlBody = "";
        private WXSession? Session = null;
        private string Path = "";
        public void InitTemplate(WXSession session)
        {
            Session = session;
            HtmlBody = "<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>WechatBakTool</title><style>p{margin:0px;}.msg{padding-bottom:10px;}.nickname{font-size:10px;}.content{font-size:14px;}</style></head><body>";

            HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\"><b>与 {0}({1}) 的聊天记录</b></p>", Session.NickName, Session.UserName);
            HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\"><b>导出时间：{0}</b></p><hr/>", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        public void InitTemplate(WXContact contact, string p)
        {
            Path = p;
            WXSession session = new WXSession();
            session.NickName = contact.NickName;
            session.UserName = contact.UserName;
            InitTemplate(session);
        }

        public void Save(string path = "")
        {

        }

        public void SetEnd()
        {
            HtmlBody += "</body></html>";
            File.AppendAllText(Path, HtmlBody);
        }

        public bool SetMsg(WXUserReader reader, WXContact contact,WorkspaceViewModel viewModel, DatetimePickerViewModel dateModel)
        {
            if (Session == null)
                throw new Exception("请初始化模版：Not Use InitTemplate");

            List<WXMsg>? msgList = reader.GetWXMsgs(contact.UserName, dateModel);
            if (msgList == null)
                throw new Exception("获取消息失败，请确认数据库读取正常");

            if(msgList.Count == 0)
            {
                viewModel.ExportCount = "没有消息，忽略";
                return false;
            }
            msgList.Sort((x, y) => x.CreateTime.CompareTo(y.CreateTime));

            bool err = false;
            int msgCount = 0;

            StreamWriter streamWriter = new StreamWriter(Path, true);
            foreach (var msg in msgList)
            {
                try
                {
                    HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\">{0} <span style=\"padding-left:10px;\">{1}</span></p>", msg.IsSender ? "我" : msg.NickName, TimeStampToDateTime(msg.CreateTime).ToString("yyyy-MM-dd HH:mm:ss"));

                    if (msg.Type == 1)
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", msg.StrContent);
                    else if (msg.Type == 3)
                    {
                        string? path = reader.GetAttachment(WXMsgType.Image, msg);
                        if (path == null)
                        {
#if DEBUG
                            File.AppendAllText("debug.log", string.Format("[D]{0} {1}:{2}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Img Error Path=>", path));
                            File.AppendAllText("debug.log", string.Format("[D]{0} {1}:{2}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Img Error Msg=>", JsonConvert.SerializeObject(msg)));
#endif
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
                    else if (msg.Type == 47)
                    {
                        string? path = reader.GetAttachment(WXMsgType.Emoji, msg);
                        if (path == null)
                        {
#if DEBUG
                            File.AppendAllText("debug.log", string.Format("[D]{0} {1}:{2}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Emoji Error Path=>", path));
                            File.AppendAllText("debug.log", string.Format("[D]{0} {1}:{2}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Emoji Error Msg=>", JsonConvert.SerializeObject(msg)));
#endif
                            HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "表情未预下载或加密表情");
                            continue;
                        }
                        HtmlBody += string.Format("<p class=\"content\"><img src=\"{0}\" style=\"max-height:300px;max-width:300px;\"/></p></div>", path);
                    }
                    else if (msg.Type == 49)
                    {
                        if (msg.SubType == 6 || msg.SubType == 40)
                        {
                            string? path = reader.GetAttachment(WXMsgType.File, msg);
                            if (path == null)
                            {
                                HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "文件不存在");
                                continue;
                            }
                            else
                            {
                                HtmlBody += string.Format("<p class=\"content\">{0}</p><p><a href=\"{1}\">点击访问</a></p></div>", "文件：" + path, path);
                            }
                        }
                        else if (msg.SubType == 19)
                        {
                            using (var decoder = LZ4Decoder.Create(true, 64))
                            {
                                byte[] target = new byte[10240];
                                int res = 0;
                                if (msg.CompressContent != null)
                                    res = LZ4Codec.Decode(msg.CompressContent, 0, msg.CompressContent.Length, target, 0, target.Length);

                                byte[] data = target.Skip(0).Take(res).ToArray();
                                string xml = Encoding.UTF8.GetString(data);
                                if (!string.IsNullOrEmpty(xml))
                                {
                                    xml = StringHelper.CleanInvalidXmlChars(xml);
                                    XmlDocument xmlObj = new XmlDocument();
                                    xmlObj.LoadXml(xml);
                                    if (xmlObj.DocumentElement != null)
                                    {
                                        string title = "";
                                        string record = "";
                                        string url = "";
                                        XmlNodeList? findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/title");
                                        if (findNode != null)
                                        {
                                            if (findNode.Count > 0)
                                            {
                                                title = findNode[0]!.InnerText;
                                            }
                                        }

                                        HtmlBody += string.Format("<p class=\"content\">{0}</p>", title);
                                        try
                                        {
                                            findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/recorditem");
                                            if (findNode != null)
                                            {
                                                if (findNode.Count > 0)
                                                {
                                                    XmlDocument itemObj = new XmlDocument();
                                                    itemObj.LoadXml(findNode[0]!.InnerText);
                                                    XmlNodeList? itemNode = itemObj.DocumentElement.SelectNodes("/recordinfo/datalist/dataitem");
                                                    if (itemNode.Count > 0)
                                                    {
                                                        foreach (XmlNode node in itemNode)
                                                        {
                                                            string nodeMsg;
                                                            string name = node["sourcename"].InnerText;
                                                            if (node.Attributes["datatype"].InnerText == "1")
                                                                nodeMsg = node["datadesc1"].InnerText;
                                                            else if (node.Attributes["datatype"].InnerText == "2")
                                                                nodeMsg = "不支持的消息";
                                                            else
                                                                nodeMsg = node["datatitle"].InnerText;
                                                            HtmlBody += string.Format("<p class=\"content\">{0}：{1}</p>", name, nodeMsg);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            HtmlBody += string.Format("<p class=\"content\">{0}</p>", "解析异常");
                                        }
                                    }
                                }
                            }
                        }
                        else if (msg.SubType == 57)
                        {
                            using (var decoder = LZ4Decoder.Create(true, 64))
                            {
                                byte[] target = new byte[10240];
                                int res = 0;
                                if (msg.CompressContent != null)
                                    res = LZ4Codec.Decode(msg.CompressContent, 0, msg.CompressContent.Length, target, 0, target.Length);

                                byte[] data = target.Skip(0).Take(res).ToArray();
                                string xml = Encoding.UTF8.GetString(data);
                                if (!string.IsNullOrEmpty(xml))
                                {
                                    xml = StringHelper.CleanInvalidXmlChars(xml);
                                    XmlDocument xmlObj = new XmlDocument();
                                    xmlObj.LoadXml(xml);
                                    if (xmlObj.DocumentElement != null)
                                    {
                                        string title = "";
                                        XmlNodeList? findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/title");
                                        if (findNode != null)
                                        {
                                            if (findNode.Count > 0)
                                            {
                                                title = findNode[0]!.InnerText;
                                            }
                                        }

                                        HtmlBody += string.Format("<p class=\"content\">{0}</p>", title);

                                        XmlNode? type = xmlObj.DocumentElement.SelectSingleNode("/msg/appmsg/refermsg/type");
                                        if(type != null)
                                        {
                                            XmlNode? source = xmlObj.DocumentElement.SelectSingleNode("/msg/appmsg/refermsg/displayname");
                                            XmlNode? text = xmlObj.DocumentElement.SelectSingleNode("/msg/appmsg/refermsg/content");
                                            if(type.InnerText == "1" && source != null && text != null)
                                            {
                                                HtmlBody += string.Format("<p class=\"content\">[引用]{0}:{1}</p>", source.InnerText, text.InnerText);
                                            }
                                            else if(type.InnerText != "1" && source != null && text != null)
                                            {
                                                HtmlBody += string.Format("<p class=\"content\">[引用]{0}:非文本消息类型-{1}</p>", source.InnerText, type);
                                            }
                                            else
                                            {
                                                HtmlBody += string.Format("<p class=\"content\">未知的引用消息</p>");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            using (var decoder = LZ4Decoder.Create(true, 64))
                            {
                                byte[] target = new byte[10240];
                                int res = 0;
                                if (msg.CompressContent != null)
                                    res = LZ4Codec.Decode(msg.CompressContent, 0, msg.CompressContent.Length, target, 0, target.Length);

                                byte[] data = target.Skip(0).Take(res).ToArray();
                                string xml = Encoding.UTF8.GetString(data);
                                if (!string.IsNullOrEmpty(xml))
                                {
                                    xml = StringHelper.CleanInvalidXmlChars(xml);
                                    XmlDocument xmlObj = new XmlDocument();
                                    xmlObj.LoadXml(xml);
                                    if (xmlObj.DocumentElement != null)
                                    {
                                        string title = "";
                                        string appName = "";
                                        string url = "";
                                        XmlNodeList? findNode = xmlObj.DocumentElement.SelectNodes("/msg/appmsg/title");
                                        if (findNode != null)
                                        {
                                            if (findNode.Count > 0)
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
                catch(Exception ex)
                {
                    err = true;
                    File.AppendAllText("Err.log", JsonConvert.SerializeObject(msg));
                    File.AppendAllText("Err.log", ex.ToString());
                }

                msgCount++;
                if(msgCount % 50 == 0)
                {
                    streamWriter.WriteLine(HtmlBody);
                    HtmlBody = "";
                    viewModel.ExportCount = msgCount.ToString();
                }
                
            }
            if(msgCount % 50 != 0)
            {
                streamWriter.WriteLine(HtmlBody);
                HtmlBody = "";
                viewModel.ExportCount = msgCount.ToString();
                if (err)
                {
                    MessageBox.Show("本次导出发生了异常，部分消息被跳过，更新至最新版本后还有此问题，请将Err.log反馈给开发，谢谢。", "错误");
                }
            }
            streamWriter.Close();
            streamWriter.Dispose();
            return true;
        }
        private static DateTime TimeStampToDateTime(long timeStamp, bool inMilli = false)
        {
            DateTimeOffset dateTimeOffset = inMilli ? DateTimeOffset.FromUnixTimeMilliseconds(timeStamp) : DateTimeOffset.FromUnixTimeSeconds(timeStamp);
            return dateTimeOffset.LocalDateTime;
        }
    }
}
