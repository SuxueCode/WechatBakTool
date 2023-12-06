using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WechatPCMsgBakTool.Model
{
    [ProtoContract]
    public class TVType
    {
        [ProtoMember(1)]
        public int Type;
        [ProtoMember(2)]
        public string TypeValue = "";
    }

    [ProtoContract]
    public class ProtoMsg
    {
        [ProtoMember(3)]
        public List<TVType>? TVMsg;
    }
}
