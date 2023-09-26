using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WechatPCMsgBakTool.Model
{
    public class ProcessInfo
    {
        public string ProcessName { get; set; } = "";
        public string ProcessId { get; set; } = "";
        public string DBPath { get; set; } = "";
    }
    public class DBInfo
    {
        public int MaxMsgDBCount { get; set; }
        public int MaxMediaDBCount { get; set;}
        public string UserPath { get; set; } = "";
        public string ResPath { get; set; } = "";
    }

    public class UserInfo
    {
        public string UserName { get; set; } = "";
        public string NickName { get; set; } = "";
    }

    public class VersionInfo
    {
        public string Version { get; set; } = "";
        public int BaseAddr { get; set; }
    }
}
