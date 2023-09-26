using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WechatPCMsgBakTool.Helpers;
using WechatPCMsgBakTool.Interface;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    /// <summary>
    /// Main.xaml 的交互逻辑
    /// </summary>
    public partial class Main : Window
    {
        private UserBakConfig? CurrentUserBakConfig = null;
        private WXUserReader? UserReader = null;
        private ObservableCollection<UserBakConfig> userBakConfigs = new ObservableCollection<UserBakConfig>();
        public Main()
        {
            InitializeComponent();
            LoadWorkspace();
        }

        private void LoadWorkspace()
        {
            userBakConfigs.Clear();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "workspace");
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                foreach(string file in files)
                {
                    string type = file.Substring(file.Length - 5, 5);
                    if(type == ".json")
                    {
                        string jsonString = File.ReadAllText(file);
                        UserBakConfig? userBakConfig = JsonConvert.DeserializeObject<UserBakConfig>(jsonString);
                        if(userBakConfig != null)
                        {
                            userBakConfigs.Add(userBakConfig);
                        }
                    }
                }
            }
            list_workspace.ItemsSource = userBakConfigs;
        }

        private void btn_decrypt_Click(object sender, RoutedEventArgs e)
        {
            if(CurrentUserBakConfig != null)
            {
                if (!CurrentUserBakConfig.Decrypt)
                {
                    byte[]? key = DecryptionHelper.GetWechatKey();
                    if (key == null)
                    {
                        MessageBox.Show("微信密钥获取失败，请检查微信是否打开,或者版本不兼容");
                        return;
                    }
                    string source = Path.Combine(CurrentUserBakConfig.UserWorkspacePath, "OriginalDB");
                    string to = Path.Combine(CurrentUserBakConfig.UserWorkspacePath, "DecDB");
                    try
                    {
                        WechatDBHelper.DecryUserData(key, source, to);
                        MessageBox.Show("解密完成，请点击读取数据");
                        CurrentUserBakConfig.Decrypt = true;
                        WXWorkspace.SaveConfig(CurrentUserBakConfig);
                        LoadWorkspace();
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("解密过程出现错误，请检查是否秘钥是否正确，如果有多开微信，请确保当前微信是选择的用户");
                    }
                }
            }
        }

        private void btn_read_Click(object sender, RoutedEventArgs e)
        {
            if(CurrentUserBakConfig == null)
            {
                MessageBox.Show("请先选择工作区");
                return;
            }
            UserReader = new WXUserReader(CurrentUserBakConfig);
            list_sessions.ItemsSource = UserReader.GetWXContacts();
        }

        private void list_workspace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentUserBakConfig = list_workspace.SelectedItem as UserBakConfig;
            if(CurrentUserBakConfig != null)
            {
                user_path.Content = "用户路径：" + CurrentUserBakConfig.UserResPath;
                if (CurrentUserBakConfig.Decrypt)
                {
                    btn_decrypt.IsEnabled = false;
                    btn_read.IsEnabled = true;
                }
                else
                {
                    btn_decrypt.IsEnabled = true;
                    btn_read.IsEnabled = false;
                }
            }
        }

        private void list_sessions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            WXContact? wXContact = list_sessions.SelectedItem as WXContact;
            if(UserReader == null)
            {
                MessageBox.Show("请先点击读取已解密工作区");
                return;
            }
            if(wXContact == null || CurrentUserBakConfig == null)
            {
                MessageBox.Show("请先选择要导出的联系人");
                return;
            }


            IExport export = new HtmlExport();
            export.InitTemplate(wXContact);
            export.SetMsg(UserReader, wXContact);
            export.SetEnd();
            //string path = UserReader.GetSavePath(wXContact);
            string path = Path.Combine(CurrentUserBakConfig.UserWorkspacePath, wXContact.UserName + ".html");
            export.Save(path);
            MessageBox.Show("导出完成");

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SelectWechat selectWechat = new SelectWechat();
            selectWechat.ShowDialog();
            if(selectWechat.SelectProcess != null)
            {
                string path = selectWechat.SelectProcess.DBPath.Replace("\\Msg\\MicroMsg.db", "");
                try
                {
                    WXWorkspace wXWorkspace = new WXWorkspace(path);
                    wXWorkspace.MoveDB();
                    MessageBox.Show("创建工作区成功");
                    LoadWorkspace();
                }
                catch (Exception)
                {
                    MessageBox.Show("创建工作区失败，请检查路径是否正确");
                }
                
            }
        }

        private void btn_search_Click(object sender, RoutedEventArgs e)
        {
            if(UserReader == null)
            {
                MessageBox.Show("请先读取工作区数据");
                return;
            }
            list_sessions.ItemsSource = UserReader.GetWXContacts(find_user.Text);
        }
    }
}
