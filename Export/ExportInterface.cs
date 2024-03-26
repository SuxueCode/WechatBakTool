using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatBakTool.Model;
using WechatBakTool.ViewModel;

namespace WechatBakTool.Export
{
    public interface IExport
    {
        void InitTemplate(WXContact session,string path);
        bool SetMsg(WXUserReader reader, WXContact session, WorkspaceViewModel viewModel, DatetimePickerViewModel dateModel);
        void SetEnd();
        void Save(string path = "");
    }
}
