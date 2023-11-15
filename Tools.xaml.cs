using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
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
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    /// <summary>
    /// Tools.xaml 的交互逻辑
    /// </summary>
    public partial class Tools : Window
    {
        public Tools()
        {
            InitializeComponent();
            LoadWorkspace();
        }

        private void LoadWorkspace()
        {
            list_workspace.Items.Clear();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "workspace");
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                foreach (string file in files)
                {
                    string type = file.Substring(file.Length - 5, 5);
                    if (type == ".json")
                    {
                        string jsonString = File.ReadAllText(file);
                        UserBakConfig? userBakConfig = null;
                        try
                        {
                            userBakConfig = JsonConvert.DeserializeObject<UserBakConfig>(jsonString);
                        }
                        catch
                        {
                            MessageBox.Show("读取到异常工作区文件，请确认备份数据是否正常\r\n文件路径：" + file, "错误");
                        }
                        if (userBakConfig != null)
                        {
                            list_workspace.Items.Add(userBakConfig);
                        }
                    }
                }
            }
        }

        private void back_video_file_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => {
                UserBakConfig? selectConfig = null;
                Dispatcher.Invoke(() => {
                    selectConfig = list_workspace.SelectedItem as UserBakConfig;
                });
                if (selectConfig != null)
                {
                    if (!selectConfig.Decrypt)
                    {
                        MessageBox.Show("工作区未解密，请先用主程序进行解密");
                        return;
                    }

                    // 检查工作区视频文件夹
                    string video_dir = Path.Combine(selectConfig.UserWorkspacePath, "Video");
                    string[] files = Directory.GetFiles(video_dir);
                    if (!Directory.Exists(video_dir))
                    {
                        Dispatcher.Invoke(() => {
                            txt_log.Text += video_dir + "不存在\r\n";
                            txt_log.ScrollToEnd();
                        });
                        return;
                    }

                    WXUserReader UserReader = new WXUserReader(selectConfig);
                    // 获取用户
                    var atc_list = UserReader.GetWXMsgAtc();
                    if(atc_list == null)
                    {
                        Dispatcher.Invoke(() => {
                            txt_log.Text += "视频列表没有内容，无法回退\r\n";
                            txt_log.ScrollToEnd();
                        });
                        return;
                    }
                    foreach (string file in files)
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        var search = atc_list.FindAll(x => x.attachPath.Contains(fileInfo.Name));
                        if (search != null)
                        {
                            WXSessionAttachInfo? select_atc = null;
                            if (search.Count > 1)
                            {
                                foreach (var s in search)
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        txt_log.Text += s + "\r\n";
                                        txt_log.ScrollToEnd();
                                    });
                                    if (s.attachPath.Contains("_raw"))
                                        select_atc = s;
                                }
                            }
                            else if (search.Count == 1)
                                select_atc = search[0];
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    txt_log.Text += "匹配不到文件\r\n";
                                    txt_log.ScrollToEnd();
                                });
                                continue;
                            }

                            if (select_atc == null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    txt_log.Text += "匹配失败\r\n";
                                    txt_log.ScrollToEnd();
                                });
                                continue;
                            }
                                
                            // 建立路径
                            string source_video_file = Path.Combine(selectConfig.UserResPath, select_atc.attachPath);
                            if (File.Exists(source_video_file))
                            {
                                Dispatcher.Invoke(() => {
                                    txt_log.Text += source_video_file + "已经存在\r\n";
                                    txt_log.ScrollToEnd();
                                });

                                continue;
                            }
                            else
                            {
                                Dispatcher.Invoke(() => {
                                    txt_log.Text += source_video_file + "开始发起回退\r\n";
                                    txt_log.ScrollToEnd();
                                });
                                File.Copy(fileInfo.FullName, source_video_file);
                            }
                        }
                    }
                }
            });
            
        }
    }
}
