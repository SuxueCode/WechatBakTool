using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WechatBakTool.ViewModel
{
    public partial class WordCloudSettingViewModel : ObservableObject
    {
        [ObservableProperty]
        private string imgHeight = "";

        [ObservableProperty]
        private string imgWidth = "";

        [ObservableProperty]
        private bool enableRemoveOneKey = true;

        [ObservableProperty]
        private string removeKey = "";

        [ObservableProperty]
        private int maxKeyCount = 200;

        [ObservableProperty]
        private string font = "微软雅黑";

        [ObservableProperty]
        private List<string> fontList = new List<string>();
    }
}
