using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool.ViewModel
{
    partial class WorkspaceViewModel : ObservableObject
    {
        [ObservableProperty]
        private WXContact? wXContact;

        [ObservableProperty]
        private ObservableCollection<WXContact>? contacts;

        private string searchString = "";
        public string SearchString
        {
            set
            {
                if (value == "搜索...")
                    searchString = "";
                else
                    searchString = value;

                OnPropertyChanged("SearchString");
            }
            get
            {
                if (searchString == "")
                    return "搜索...";
                return searchString;
            }
        }

        public string SearchRealString
        {
            get
            {
                return searchString;
            }
        }
    }
}
