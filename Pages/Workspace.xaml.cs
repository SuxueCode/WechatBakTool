using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WechatBakTool.Model;
using WechatBakTool.ViewModel;

namespace WechatBakTool.Pages
{
    /// <summary>
    /// Workspace.xaml 的交互逻辑
    /// </summary>
    public partial class Workspace : Page
    {
        public WXUserReader? UserReader { get; set; }
        private WorkspaceViewModel ViewModel { get; set; } = new WorkspaceViewModel();
        public Workspace()
        {
            InitializeComponent();
            DataContext = ViewModel;
            UserBakConfig? config = Main2.CurrentUserBakConfig;
            if (config != null)
            {
                UserReader = new WXUserReader(config);
                if (config.Decrypt)
                {
                    ViewModel.Contacts = UserReader.GetWXContacts();
                }
            }
        }

        private void btn_read_Click(object sender, RoutedEventArgs e)
        {
            if (Main2.CurrentUserBakConfig == null)
            {
                MessageBox.Show("工作区配置加载失败，请检查配置文件是否正常","错误");
                return;
            }
        }

        private void list_users_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.WXContact = list_users.SelectedItem as WXContact;
            if(ViewModel.WXContact == null || UserReader == null)
            {
                return;
            }
            List<WXMsg>? msgs = UserReader.GetWXMsgs(ViewModel.WXContact.UserName);
            list_msg.ItemsSource = msgs;
        }

        private void txt_find_user_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (UserReader == null)
                return;

            string findName = txt_find_user.Text;
            if (txt_find_user.Text == "搜索...")
                findName = "";

            ViewModel.Contacts = UserReader.GetWXContacts(findName);
        }

        private void txt_find_user_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txt_find_user.Text == "搜索...")
                txt_find_user.Text = "";

            Debug.WriteLine(ViewModel.SearchString);
        }
    }
}
