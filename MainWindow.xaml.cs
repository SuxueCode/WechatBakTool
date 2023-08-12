using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WechatPCMsgBakTool.Helpers;
using WechatPCMsgBakTool.Interface;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string UserMsgPath { get; set; } = "";
        public MainWindow()
        {
            InitializeComponent();
        }

        private void select_user_msg_path_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(txt_user_msg_path.Text))
            {
                UserMsgPath = txt_user_msg_path.Text;
                if (UserMsgPath.Substring(UserMsgPath.Length - 1, 1) == "\\") {
                    UserMsgPath = UserMsgPath.Substring(0, UserMsgPath.Length - 1);
                }

                //判定数据目录是否存在
                if (Directory.Exists(UserMsgPath + "\\Msg"))
                {
                    //MessageBox.Show("微信目录存在");
                }

                //复制数据DB
                WechatDBHelper.CreateUserWorkPath(UserMsgPath);
                string err = WechatDBHelper.MoveUserData(UserMsgPath);
                if(err != "")
                {
                    MessageBox.Show(err);
                    return;
                }
                else
                {
                    MessageBox.Show("用户目录创建成功，请打开PC微信并登录，获取数据库秘钥解密");
                }
            }
        }

        private void decryption_user_msg_db_Click(object sender, RoutedEventArgs e)
        {
            byte[]? key = DecryptionHelper.GetWechatKey();
            if(key == null)
            {
                MessageBox.Show("微信密钥获取失败，请检查微信是否打开,或者版本不兼容");
                return;
            }
            WechatDBHelper.DecryUserData(key);
            MessageBox.Show("解密完成，请点击读取数据");
        }

        WXReader? Reader = null;
        private void read_user_msg_db_Click(object sender, RoutedEventArgs e)
        {
            list_sessions.Items.Clear();
            if (cb_use_local_decdb.IsChecked == true)
            {
                DBInfo info = WechatDBHelper.GetDBinfoOnLocal(txt_user_msg_path.Text);
                Reader = new WXReader(info);
            }
            else
            {
                Reader = new WXReader();
            }
            
            List<WXSession>? sessions = new List<WXSession>();
            sessions = Reader.GetWXSessions();
            if (sessions == null)
            {
                MessageBox.Show("咩都厶啊");
                return;
            }

            foreach (WXSession session in sessions)
            {
                list_sessions.Items.Add(session);
            }
        }

        private bool loading = false;
        private bool end = false;
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender == null)
                return;
            ScrollViewer scrollViewer = (ScrollViewer)sender;
            if (scrollViewer.ScrollableHeight == 0)
                return;
            if (scrollViewer.ScrollableHeight - scrollViewer.ContentVerticalOffset < 10)
            {
                if (!loading && !end)
                {
                    loading = true;
                    //GetMsg();
                }
            }
        }

        private void list_sessions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void export_record_Click(object sender, RoutedEventArgs e)
        {
            WXSession selectItem = (WXSession)list_sessions.SelectedValue;
            if (selectItem != null)
            {
                IExport export = new HtmlExport();
                export.InitTemplate(selectItem);

                if(Reader == null)
                {
                    MessageBox.Show("请先读取用户数据");
                    return;
                }
                
                export.SetMsg(Reader, selectItem);
                export.SetEnd();

                string path = Reader.GetSavePath(selectItem);
                export.Save(path);
                MessageBox.Show("导出完成");
            }
        }

        private void find_session_person_Click(object sender, RoutedEventArgs e)
        {
            list_sessions.Items.Clear();
            if (Reader == null)
                Reader = new WXReader();

            List<WXContact>? sessions = new List<WXContact>();
            sessions = Reader.GetUser(txt_find_session.Text);
            if (sessions == null)
            {
                MessageBox.Show("咩都厶啊");
                return;
            }

            foreach (WXContact session in sessions)
            {
                WXSession session1 = new WXSession();
                session1.NickName = session.NickName;
                session1.UserName = session.UserName;
                list_sessions.Items.Add(session1);
            }
        }
    }
}
