using System;
using System.Collections.Generic;
using SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Media.Imaging;

namespace WechatBakTool.Model
{
    public class UserBakConfig
    {
        public string UserResPath { get; set; } = "";
        public string UserWorkspacePath { get; set; } = "";
        public bool Decrypt { get; set; } = false;
        public string DecryptStatus
        {
            get { return Decrypt ? "已解密" : "未解密"; }
        }
        public string Hash { get; set; } = "";
        public string NickName { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Account { get; set; } = "";
        public string Friends_Number { get; set; } = "-";
        public string Msg_Number { get; set; } = "-";
        public string Key { get; set; } = "";
        public bool Manual { get; set; } = false;
    }

    public class WXCount
    {
        public int Count { get; set; }
    }

    public class WXMsgGroup
    {
        [Column("StrTalker")]
        public string UserName { get; set; } = "";

        [Column("MsgCount")]
        public int MsgCount { get; set; }
        public string NickName { get; set; } = "";
    }

    [Table("ContactHeadImg1")]
    public class ContactHeadImg
    {
        public string usrName { get; set; } = "";
        public int createTime { get; set; }
        public byte[]? smallHeadBuf { get; set; }
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
        [Column("MsgSequence")]
        public int MsgSequence { get; set; }
        [Column("Type")]
        public int Type { get; set; }
        [Column("SubType")]
        public int SubType { get; set; }
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
        public string DisplayContent { get; set; } = "";
        [Column("CompressContent")]
        public byte[]? CompressContent { get; set; }
        [Column("BytesExtra")]
        public byte[]? BytesExtra { get; set; }
        public string NickName { get; set; } = "";
    }

    [Table("ChatRoom")]
    public class WXChatRoom
    {
        [Column("ChatRoomName")]
        public string ChatRoomName { get; set; } = "";
        [Column("UserNameList")]
        public string UserNameList { get; set; } = "";
        [Column("DisplayNameList")]
        public string DisplayNameList { get; set; } = "";
        [Column("RoomData")]
        public byte[]? RoomData { get; set; }

    }

    [Table("Media")]
    public class WXMediaMsg
    {
        public int Key { get; set; }
        public byte[]? Buf { get; set; }
        public string Reserved0 { get; set; } = "";
    }

    public class WXContactHT
    {
        public string UserName { get; set; } = "";
        public string NickName { get; set; } = "";
        public string LastMsg { get; set; } = "";
        public int FileCount { get; set; } = 1;
        public string AvatarString { get; set; } = "";
        public bool Hidden { get; set; } = false;
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
        [Column("strContent")]
        public string LastMsg { get; set; } = "";
        [Column("ExtraBuf")]
        public byte[]? ExtraBuf { get; set; }
        public BitmapImage? Avatar { get; set; }
        [Column("Remark")]
        public string Remark { get; set; } = "";
    }

    [Table("ContactHeadImgUrl")]
    public class WXUserImg {
        [Column("usrName")]
        public string UserName { get; set; } = "";
        [Column("smallHeadImgUrl")]
        public string SmallImg { get; set; } = "";
        [Column("bigHeadImgUrl")]
        public string BigImg { get; set; } = "";
    }
}
