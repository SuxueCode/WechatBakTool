using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WechatBakTool.Model;

namespace WechatBakTool.ViewModel
{
    public partial class WorkspaceViewModel : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectContact))]
        [NotifyPropertyChangedFor(nameof(LabelStatus))]
        private WXContact? wXContact = null;

        [ObservableProperty]
        private ObservableCollection<WXMsg> wXMsgs = new ObservableCollection<WXMsg>();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LabelStatus))]
        private string exportCount = "";

        public string LabelStatus
        {
            get
            {
                if (WXContact == null)
                    return ExportCount;

                string name = WXContact.NickName;
                if(WXContact.Remark != "")
                    name = WXContact.Remark;

                return string.Format("{0}:{1}", name, ExportCount);
            }
        }

        public bool SelectContact
        {
            get
            {
                if (WXContact == null)
                    return false;
                else
                    return true;
            }
        }
        [ObservableProperty]
        private ObservableCollection<WXContact>? contacts;

        [ObservableProperty]
        private ObservableCollection<ExportItem>? exportItems;

        [ObservableProperty]
        private ExportItem? selectExportItem;

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
    }
}
