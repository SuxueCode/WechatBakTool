using System;
using System.Collections.Generic;
using SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace WechatPCMsgBakTool.Model
{
    public class UserBakConfig : INotifyPropertyChanged
    {
        public string UserResPath { get; set; } = "";
        public string UserWorkspacePath { get; set; } = "";
        public bool Decrypt { get; set; } = false;
        public string Hash { get; set; } = "";
        public string NickName { get; set; } = "";
        public string UserName { get; set; } = "";

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class WXMsgGroup
    {
        [Column("StrTalker")]
        public string UserName { get; set; } = "";

        [Column("MsgCount")]
        public int MsgCount { get; set; }
        public string NickName { get; set; } = "";
    }

    public class WXUserInfo
    {
        public string UserName { get; set; } = "";
        public string Alias { get; set; } = "";
        public int DelFlag { get; set; }
        public int Flag { get; set; }
        public string NickName { get; set; } = "";
        public string LabelIDList { get; set; } = "";
    }

    public class WXUserImgUrl
    {
        [Column("usrName")]
        public string UserName { get; set; } = "";

        [Column("bigHeadImgUrl")]
        public string Img { get; set; } = "";
    }

    [Table("Session")]
    public class WXSession
    {
        [Column("strUsrName")]
        public string UserName { get; set; } = "";
        [Column("nOrder")]
        public long Order { get; set; }
        [Column("strNickName")]
        public string NickName { get; set; } = "";
        [Column("strContent")]
        public string Content { get; set; } = "";
        [Column("nTime")]
        public int LastTime { get; set; }
        public int ReadCount { get; set; }
        public int LastMsgId { get; set; }
    }

    [Table("SessionAttachInfo")]
    public class WXSessionAttachInfo
    {
        [Column("attachId")]
        public int AtcId { get; set; }
        [Column("msgType")]
        public int MsgType { get; set; }
        [Column("msgId")]
        public string msgId { get; set; } = "";
        [Column("msgTime")]
        public long msgTime { get; set; }
        [Column("attachPath")]
        public string attachPath { get; set; } = "";
        [Column("attachSize")]
        public int attachSize { get; set; }
    }
    [Table("Media")]
    public class WXMedia
    {
        public string Key { get; set; } = "";
        public string Reserved0 { get; set; } = "";
        public byte[]? Buf { get; set; }
    }
    [Table("MSG")]
    public class WXMsg
    {
        [Column("localId")]
        public int LocalId { get; set; }
        [Column("Type")]
        public int Type { get; set; }
        [Column("CreateTime")]
        public long CreateTime { get; set; }
        [Column("IsSender")]
        public bool IsSender { get; set; }
        [Column("MsgSvrID")]
        public string MsgSvrID { get; set; } = "";
        [Column("StrTalker")]
        public string StrTalker { get; set; } = "";
        [Column("StrContent")]
        public string StrContent { get; set; } = "";
        [Column("CompressContent")]
        public byte[]? CompressContent { get; set; }
    }

    [Table("Media")]
    public class WXMediaMsg
    {
        public int Key { get; set; }
        public byte[]? Buf { get; set; }
        public string Reserved0 { get; set; } = "";
    }

    [Table("Contact")]
    public class WXContact
    {
        [Column("UserName")]
        public string UserName { get; set; } = "";
        [Column("Alias")]
        public string Alias { get; set; } = "";
        [Column("NickName")]
        public string NickName { get; set; } = "";
    }
}
