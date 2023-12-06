using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatBakTool.Model;

namespace WechatBakTool.Export
{
    public interface IExport
    {
        void InitTemplate(WXContact session,string path);
        void SetMsg(WXUserReader reader, WXContact session);
        void SetEnd();
        void Save(string path = "", bool append = false);
    }
}
