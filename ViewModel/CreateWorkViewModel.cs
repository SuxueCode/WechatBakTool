using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool.ViewModel
{
    partial class CreateWorkViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<ProcessInfo> processInfos = new List<ProcessInfo>();

        [ObservableProperty]
        private ProcessInfo? selectProcess;

        [ObservableProperty]
        private string userName = "";
    }
}
