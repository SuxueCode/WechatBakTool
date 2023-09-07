using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatPCMsgBakTool.Interface;
using WechatPCMsgBakTool.Model;

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

        public void SetMsg(WXReader reader, WXSession session)
        {
            List<WXMsg> msgList = reader.GetMsgs(session.UserName);
            msgList.Sort((x, y) => x.CreateTime.CompareTo(y.CreateTime));
            foreach (var msg in msgList)
            {
                if (Session == null)
                    throw new Exception("请初始化模版：Not Use InitTemplate");
                HtmlBody += string.Format("<div class=\"msg\"><p class=\"nickname\">{0} <span style=\"padding-left:10px;\">{1}</span></p>", msg.IsSender ? "我" : Session.NickName, TimeStampToDateTime(msg.CreateTime).ToString("yyyy-MM-dd HH:mm:ss"));

                if (msg.Type == 1)
                    HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", msg.StrContent);
                else if(msg.Type == 3)
                {
                    string? path = reader.GetImage(msg);
                    if (path == null)
                    {
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "图片转换出现错误或文件不存在");
                        continue;
                    }
                    HtmlBody += string.Format("<p class=\"content\"><img src=\"{0}\" style=\"max-height:1000px;max-width:1000px;\"/></p></div>", path);
                }
                else if(msg.Type == 43)
                {
                    string? path = reader.GetVideo(msg);
                    if (path == null)
                    {
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "视频不存在");
                        continue;
                    }
                    HtmlBody += string.Format("<p class=\"content\"><video controls style=\"max-height:300px;max-width:300px;\"><source src=\"{0}\" type=\"video/mp4\" /></video></p></div>", path);
                }
                else if(msg.Type == 34)
                {
                    string? path = reader.GetVoice(msg);
                    if (path == null)
                    {
                        HtmlBody += string.Format("<p class=\"content\">{0}</p></div>", "视频不存在");
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
