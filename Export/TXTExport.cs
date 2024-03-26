using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using WechatBakTool.Model;
using WechatBakTool.ViewModel;

namespace WechatBakTool.Export
{
    public class TXTExport : IExport
    {
        private WXContact? Contact { get; set; } = null;
        private string Path { get; set; } = "";
        public void InitTemplate(WXContact contact,string p)
        {
            Contact = contact;
            Path = p;
            if (File.Exists(Path))
            {
                File.WriteAllText(Path, "");
            }
            File.AppendAllText(Path, string.Format("WechatBakTool\n"));
            File.AppendAllText(Path, string.Format("与 {0} 的聊天记录\n", Contact.NickName));
            File.AppendAllText(Path, string.Format("导出时间：{0}\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            File.AppendAllText(Path, string.Format("=================================================================\n\n\n"));
        }

        void IExport.Save(string path)
        {
            
        }

        void IExport.SetEnd()
        {
            
        }

        public bool SetMsg(WXUserReader reader, WXContact session, WorkspaceViewModel viewModel, DatetimePickerViewModel dateModel)
        {
            if (Contact == null)
                throw new Exception("请初始化模版：Not Use InitTemplate");

            List<WXMsg>? msgList = reader.GetWXMsgs(Contact.UserName, dateModel);
            if (msgList == null)
                throw new Exception("获取消息失败，请确认数据库读取正常");

            msgList.Sort((x, y) => x.CreateTime.CompareTo(y.CreateTime));

            int msgCount = 0;
            foreach (var msg in msgList)
            {
                string txtMsg = "";
                switch (msg.Type)
                {
                    case 1:
                        txtMsg = msg.StrContent;
                        break;
                    case 3:
                        txtMsg = "[图片]";
                        break;
                    case 34:
                        txtMsg = "[语音]";
                        break;
                    case 43:
                        txtMsg = "[视频]";
                        break;
                    case 49:
                        if (msg.SubType == 6 || msg.SubType == 19 || msg.SubType == 40)
                        {
                            txtMsg = "[文件]";
                        }
                        else
                        {
                            try
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
                                        xml = xml.Replace("\n", "");
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
                                            txtMsg = string.Format("{0},标题：{1},链接：{2}", appName, title, url);
                                        }
                                        else
                                        {
                                            txtMsg = "[分享链接出错了]";
                                        }
                                    }
                                    else
                                    {
                                        txtMsg = "[分享链接出错了]";
                                    }
                                }
                            }
                            catch
                            {
                                txtMsg = "[分享链接出错了]";
                            }
                        }
                        break;
                }
                string row = string.Format("{2} | {0}:{1}\n", msg.IsSender ? "我" : msg.NickName, txtMsg, TimeStampToDateTime(msg.CreateTime).ToString("yyyy-MM-dd HH:mm:ss"));
                File.AppendAllText(Path, row);
                msgCount++;
                viewModel.ExportCount = msgCount.ToString();
            }
            return true;
        }

        private static DateTime TimeStampToDateTime(long timeStamp, bool inMilli = false)
        {
            DateTimeOffset dateTimeOffset = inMilli ? DateTimeOffset.FromUnixTimeMilliseconds(timeStamp) : DateTimeOffset.FromUnixTimeSeconds(timeStamp);
            return dateTimeOffset.LocalDateTime;
        }
    }
}
