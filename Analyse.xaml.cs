using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    /// <summary>
    /// Analyse.xaml 的交互逻辑
    /// </summary>
    public partial class Analyse : Window
    {
        private UserBakConfig UserBakConfig;
        private WXUserReader UserReader;
        public Analyse(UserBakConfig userBakConfig,WXUserReader reader)
        {
            UserBakConfig = userBakConfig;
            UserReader = reader;
            InitializeComponent();
        }

        private void btn_analyse_Click(object sender, RoutedEventArgs e)
        {
            List<WXContact>? contacts = UserReader.GetWXContacts();
            List<WXMsgGroup> list = UserReader.GetWXMsgGroup().OrderByDescending(x => x.MsgCount).ToList();
            if(contacts == null)
                contacts = new List<WXContact>();

            foreach (WXMsgGroup item in list)
            {
                WXContact? contact = contacts.Find(x => x.UserName == item.UserName);
                if (contact != null)
                {
                    item.NickName = contact.NickName;
                }
                else
                    item.NickName = "已删除人员：" + item.UserName;
            }
            list_msg_group.ItemsSource = list;
        }

        private void btn_copy_id_Click(object sender, RoutedEventArgs e)
        {
            WXMsgGroup? msgGroup = list_msg_group.SelectedItem as WXMsgGroup;
            if(msgGroup == null)
            {
                MessageBox.Show("请先选择数据");
                return;
            }
            else
            {
                Clipboard.SetDataObject(msgGroup.UserName);
            }
            
        }

        private void list_msg_group_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WXMsgGroup? wXMsgGroup = list_msg_group.SelectedItem as WXMsgGroup;
            if(wXMsgGroup != null)
            {
                List<WXMsg>? wXMsgs = UserReader.GetWXMsgs(wXMsgGroup.UserName);
                if(wXMsgs != null)
                {
                    wXMsgs = wXMsgs.OrderByDescending(x => x.CreateTime).ToList();
                    list_msg_search.ItemsSource = wXMsgs;
                }
                    
            }
        }

        private void btn_search_Click(object sender, RoutedEventArgs e)
        {
            List<WXMsg>? wXMsgs = UserReader.GetWXMsgs("",txt_search_text.Text);
            if (wXMsgs != null)
            {
                wXMsgs = wXMsgs.OrderByDescending(x => x.CreateTime).ToList();
                list_msg_search.ItemsSource = wXMsgs;
            }
        }

        private void btn_search_copy_id_Click(object sender, RoutedEventArgs e)
        {
            WXMsg? wxMsg = list_msg_search.SelectedItem as WXMsg;
            if (wxMsg == null)
            {
                MessageBox.Show("请先选择数据");
                return;
            }
            else
            {
                Clipboard.SetDataObject(wxMsg.StrTalker);
            }
        }
    }
}
