using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool.Interface
{
    public interface IExport
    {
        void InitTemplate(WXSession session);
        void InitTemplate(WXContact session);
        void SetMsg(WXUserReader reader, WXContact session);
        void SetEnd();
        void Save(string path = "", bool append = false);

    }
}
