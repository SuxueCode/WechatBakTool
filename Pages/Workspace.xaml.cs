using JiebaNet.Segmenter;
using JiebaNet.Segmenter.Common;
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
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using WechatBakTool.Export;
using WechatBakTool.Helpers;
using WechatBakTool.Model;
using WechatBakTool.ViewModel;
using WordCloudSharp;
using System.Drawing;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace WechatBakTool.Pages
{
    /// <summary>
    /// Workspace.xaml 的交互逻辑
    /// </summary>
    public partial class Workspace : Page
    {
        public WXUserReader? UserReader;
        private WorkspaceViewModel ViewModel = new WorkspaceViewModel();
        public Workspace()
        {
            ViewModel.ExportItems = new System.Collections.ObjectModel.ObservableCollection<ExportItem> {
                new ExportItem(){ Name="导出为HTML",Value=1 },
                new ExportItem(){ Name="导出为TXT",Value=2 },
                new ExportItem(){ Name="与他的词云",Value=3 },
            };
            ViewModel.SelectExportItem = ViewModel.ExportItems[0];
            InitializeComponent();

            list_users.Items.Clear();

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
            ViewModel.ExportCount = "";
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

        private void btn_export_Click(object sender, RoutedEventArgs e)
        {
            if(ViewModel.WXContact == null || UserReader == null)
            {
                MessageBox.Show("请选择联系人", "错误");
                return;
            }
            try
            {
                string path = Path.Combine(Main2.CurrentUserBakConfig!.UserWorkspacePath, ViewModel.WXContact.UserName + ".txt");
                IExport export = new TXTExport();
                export.InitTemplate(ViewModel.WXContact, path);
                export.SetMsg(UserReader, ViewModel.WXContact, ViewModel);
                export.SetEnd();
                export.Save(path);
            }catch(Exception ex)
            {
                File.AppendAllText("1.log", ex.Message);
                MessageBox.Show(ex.Message);
            }
            
            MessageBox.Show("导出完成");
        }

        private void btn_open_workspace_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe ", Main2.CurrentUserBakConfig!.UserWorkspacePath);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                if (ViewModel.WXContact == null || UserReader == null)
                {
                    MessageBox.Show("请选择联系人", "错误");
                    return;
                }
                if (ViewModel.SelectExportItem == null)
                {
                    MessageBox.Show("请选择导出方式", "错误");
                    return;
                }
                if(ViewModel.SelectExportItem.Value == 3)
                {
                    if(UserReader != null && ViewModel.WXContact != null)
                    {
                        System.Drawing.Image? mask = null;
                        if (File.Exists("mask.png"))
                            mask = System.Drawing.Image.FromFile("mask.png");

                        WordCloudSettingViewModel setting = new WordCloudSettingViewModel() {
                            ImgWidth = mask == null ? "1000": mask.Width.ToString(),
                            ImgHeight = mask == null ? "1000" : mask.Height.ToString(),
                            EnableRemoveOneKey = true,
                        };
                        Dispatcher.Invoke(() => {
                            WordCloudSetting wordCloudSetting = new WordCloudSetting(setting);
                            wordCloudSetting.ShowDialog();
                        });
                        
                        var jieba = new JiebaSegmenter();
                        Counter<string> counter = new Counter<string>();

                        try
                        {
                            ViewModel.ExportCount = "词频统计ing...";
                            List<WXMsg>? msgs = UserReader.GetWXMsgs(ViewModel.WXContact.UserName);
                            if (msgs != null)
                            {
                                foreach (WXMsg msg in msgs)
                                {
                                    if (msg.Type == 1)
                                    {
                                        List<string> list = jieba.Cut(msg.StrContent).ToList();
                                        counter.Add(list);
                                    }

                                }
                            }
                        }
                        catch
                        {
                            ViewModel.ExportCount = "异常";
                            MessageBox.Show("词频统计发生异常，请检查字典文件是否存在", "错误");
                            return;
                        }
                        
                        var orderBy = counter.MostCommon();
                        ViewModel.ExportCount = "移除部分词语...";
                        string[] remove_string_list = setting.RemoveKey.Split(",");
                        foreach(string remove_string in remove_string_list)
                        {
                            counter.Remove(remove_string);
                        }
                        foreach(var key in orderBy)
                        {
                            if (key.Key.Length == 1 && setting.EnableRemoveOneKey)
                                counter.Remove(key.Key);
                        }

                        ViewModel.ExportCount = "渲染词云结果";
                        string resultPath = "result.jpg";

                        WordCloud wordCloud;
                        if(mask != null)
                            wordCloud = new WordCloud(int.Parse(setting.ImgWidth), int.Parse(setting.ImgHeight), mask: mask, allowVerical: true, fontname: setting.Font);
                        else
                            wordCloud = new WordCloud(int.Parse(setting.ImgWidth), int.Parse(setting.ImgHeight), allowVerical: true, fontname: setting.Font);

                        if (orderBy.Count() >= setting.MaxKeyCount)
                            orderBy = orderBy.Take(setting.MaxKeyCount);

                        var result = wordCloud.Draw(orderBy.Select(it => it.Key).ToList(), orderBy.Select(it => it.Value).ToList());
                        result.Save(resultPath);
                        ViewModel.ExportCount = "完成";
                        MessageBox.Show("生成完毕，请查看软件根目录result.jpg", "提示");
                    }
                    return;
                }

                string name = ViewModel.WXContact.NickName;
                name = name.Replace(@"\", "");
                name = Regex.Replace(name, "[ \\[ \\] \\^ \\-_*×――(^)$%~!/@#$…&%￥—+=<>《》|!！??？:：•`·、。，；,.;\"‘’“”-]", "");
                string path = Path.Combine(
                    Main2.CurrentUserBakConfig!.UserWorkspacePath,
                    string.Format(
                        "{0}-{1}",
                        ViewModel.WXContact.UserName,
                        ViewModel.WXContact.Remark == "" ? name : ViewModel.WXContact.Remark
                    )
                );
                IExport export;

#if DEBUG
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();
#endif
                if (ViewModel.SelectExportItem.Value == 2)
                {
                    path += ".txt";
                    export = new TXTExport();
                }
                else
                {
                    path += ".html";
                    export = new HtmlExport();
                }
                export.InitTemplate(ViewModel.WXContact, path);
                export.SetMsg(UserReader, ViewModel.WXContact, ViewModel);
                export.SetEnd();
                export.Save(path);
#if DEBUG
                stopwatch.Stop();
                MessageBox.Show(stopwatch.Elapsed.ToString());
#endif
                MessageBox.Show("导出完成", "提示");
            });
            
        }

        private void btn_pre_emoji_Click(object sender, RoutedEventArgs e)
        {
            if(UserReader != null && ViewModel.WXContact != null)
            {
                Task.Run(() => {
                    UserReader.PreDownloadEmoji(ViewModel.WXContact.UserName);
                    MessageBox.Show("用户所有表情预下载完毕");
                });
            }
        }
    }
}
