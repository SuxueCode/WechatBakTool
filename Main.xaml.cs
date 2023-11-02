using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Encoders;
using K4os.Compression.LZ4.Streams;
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
            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;
            InitializeComponent();
            LoadWorkspace();
            this.Title += $" {Application.ResourceAssembly.GetName().Version}";
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("发生了未知错误，记录已写入到根目录err.log，如果可以，欢迎反馈给开发人员，非常感谢", "错误");
            File.AppendAllText("err.log", "\r\n\r\n\r\n=============================\r\n");
            File.AppendAllText("err.log", string.Format("异常时间：{0}\r\n", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            File.AppendAllText("err.log", e.Exception.ToString());
            return;
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
                        UserBakConfig? userBakConfig = null;
                        try
                        {
                            userBakConfig = JsonConvert.DeserializeObject<UserBakConfig>(jsonString);
                        }
                        catch
                        {
                            MessageBox.Show("读取到异常工作区文件，请确认备份数据是否正常\r\n文件路径：" + file,"错误");
                        }
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
                    byte[]? key = null;
                    try
                    {
                         key = DecryptionHelper.GetWechatKey();
                    }
                    catch (Exception ex)
                    {
                        if(ex.Source == "Newtonsoft.Json")
                        {
                            MessageBox.Show("版本文件读取失败，请检查版本文件内容是否为正确的json格式", "错误");
                        }
                        else
                        {
                            MessageBox.Show(ex.Message);
                        }
                        return;
                    }
                    //byte[]? key = DecryptionHelper.GetWechatKey();
                    if (key == null)
                    {
                        MessageBox.Show("微信密钥获取失败，请检查微信是否打开,或者版本不兼容");
                        return;
                    }
                    string key_string = BitConverter.ToString(key, 0).Replace("-", string.Empty).ToLower().ToUpper();
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
                    catch (Exception ex)
                    {
                        MessageBox.Show("解密过程出现错误：" + ex.Message);
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
            if(cb_del_search.IsChecked != null)
            {
                if (!(bool)cb_del_search.IsChecked)
                    list_sessions.ItemsSource = UserReader.GetWXContacts(find_user.Text);
                else
                {
                    List<WXMsg>? wXMsgs = UserReader.GetWXMsgs(find_user.Text);
                    if(wXMsgs != null)
                    {
                        if(wXMsgs.Count > 0)
                        {
                            List<WXContact> wXContacts = new List<WXContact>() { new WXContact() { NickName = wXMsgs[0].StrTalker, UserName = wXMsgs[0].StrTalker } };
                            list_sessions.ItemsSource = wXContacts;
                        }
                    }
                }

            }
            
        }

        private void btn_analyse_Click(object sender, RoutedEventArgs e)
        {
            if(UserReader == null || CurrentUserBakConfig == null)
            {
                MessageBox.Show("请先读取数据");
                return;
            }
            Analyse analyse = new Analyse(CurrentUserBakConfig, UserReader);
            analyse.Show();
        }
    }
}
