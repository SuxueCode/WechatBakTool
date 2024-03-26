using JiebaNet.Segmenter.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using WechatBakTool.Helpers;
using WechatBakTool.Model;
using WechatBakTool.ViewModel;

namespace WechatBakTool.Pages
{
    /// <summary>
    /// CreateWork.xaml 的交互逻辑
    /// </summary>
    public partial class CreateWork : Page
    {
        private CreateWorkViewModel ViewModel = new CreateWorkViewModel();
        public CreateWork()
        {
            DataContext = ViewModel;
            
            InitializeComponent();
            GetWechatProcessInfos();
            isManualProcess();
        }
        
        private void isManualProcess()
        {
            if(Main2.CurrentUserBakConfig!= null)
            {
                cb_manual.IsChecked = Main2.CurrentUserBakConfig.Manual;
            }
        }
        

        private void GetWechatProcessInfos()
        {
            ViewModel.ProcessInfos.Clear();
            Process[] processes = Process.GetProcessesByName("wechat");
            foreach (Process p in processes)
            {
                var lHandles = NativeAPIHelper.GetHandleInfoForPID((uint)p.Id);
                foreach (var h in lHandles)
                {
                    string name = NativeAPIHelper.FindHandleName(h, p);
                    if (name != "")
                    {
                        // 预留handle log
                        if (File.Exists("handle.log"))
                        {
                            File.AppendAllText("handle.log", string.Format("{0}|{1}|{2}|{3}\n", p.Id, h.ObjectTypeIndex, h.HandleValue, name));
                        }
                        if (name.Contains("\\MicroMsg.db") && name.Substring(name.Length - 3, 3) == ".db")
                        {
                            ProcessInfo info = new ProcessInfo();
                            info.ProcessId = p.Id.ToString();
                            info.ProcessName = p.ProcessName;
                            info.DBPath = DevicePathMapper.FromDevicePath(name);
                            ViewModel.ProcessInfos.Add(info);
                        }
                    }
                }
            }
        }

        private void list_process_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel.SelectProcess != null)
            {
                string[] name_raw = ViewModel.SelectProcess.DBPath.Split("\\");
                ViewModel.UserName = name_raw[name_raw.Length - 3];

                FileInfo fileInfo = new FileInfo(ViewModel.SelectProcess.DBPath);
                DirectoryInfo msgParent = fileInfo.Directory!.Parent!;
                DirectoryInfo[] accounts = msgParent.GetDirectories();

                DirectoryInfo? newUserName = null;
                foreach ( DirectoryInfo account in accounts )
                {
                    if(account.Name.Contains("account_")) {
                        if(newUserName == null)
                            newUserName = account;
                        else
                        {
                            if (newUserName.LastWriteTime < account.LastWriteTime)
                                newUserName = account;
                        }
                    }
                }
                if(newUserName != null)
                {
                    ViewModel.UserName = newUserName.Name.Split("_")[1];
                }
            }
        }

        private void btn_create_worksapce_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.IsEnable = false;
            bool m = (bool)cb_manual.IsChecked!;
            Task.Run(() => {
                if (ViewModel.KeyType != -1 && !m)
                {
                    if (ViewModel.SelectProcess != null)
                    {
                        ViewModel.LabelStatus = "数据准备";
                        string path = ViewModel.SelectProcess.DBPath.Replace("\\Msg\\MicroMsg.db", "");
                        try
                        {
                            ViewModel.LabelStatus = "准备创建工作区";
                            //创建工作区
                            WXWorkspace wXWorkspace = new WXWorkspace(path, ViewModel.UserName);
                            //DB移动
                            wXWorkspace.MoveDB(ViewModel);
                            if(ViewModel.SelectProcess == null)
                                return;

                            //开始解密数据库
                            try
                            {
                                ViewModel.LabelStatus = "开始解密数据库";
                                wXWorkspace.DecryptDB(ViewModel.SelectProcess.ProcessId, ViewModel.KeyType,ViewModel);

                                MessageBox.Show("创建工作区成功");
                                Dispatcher.Invoke(() =>
                                {
                                    ((Main2)Window.GetWindow(this)).LoadWorkspace();
                                });
                                
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                                ViewModel.IsEnable = true;
                            }
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("创建工作区失败，请检查路径是否正确");
                            ViewModel.IsEnable = true;
                        }
                    }
                }
                else if (m)
                {
                    WXWorkspace wXWorkspace = new WXWorkspace(Main2.CurrentUserBakConfig!);
                    ViewModel.LabelStatus = "开始解密数据库";
                    wXWorkspace.DecryptDB("", -1, ViewModel,Main2.CurrentUserBakConfig!.Key);
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("解密完成");
                        ((Main2)Window.GetWindow(this)).LoadWorkspace();
                    });
                }
                else
                {
                    MessageBox.Show("请选择Key获取方式", "错误");
                }
                ViewModel.IsEnable = true;
            });
        }

        private void cb_manual_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("该功能仅限用于网络安全研究用途使用，红队同学请在合规授权下进行相关操作","重要提醒！！！！！！！！！");
            if(Main2.CurrentUserBakConfig != null)
            {
                if (Main2.CurrentUserBakConfig.Manual)
                {
                    return;
                }
            }
            if (MessageBox.Show("我确认获取到合规授权，仅用于网络安全用途使用", "信息确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (File.Exists("auth.txt"))
                {
                    string auth = File.ReadAllText("auth.txt");
                    /* 
                     * 
                     * pwd: 
                     * 我已知晓手动模式可能潜在的法律及道德风险，我明白非法使用将要承担相关法律责任。
                     * tips:
                     * 请不要公开宣传手动模式，不提供任何使用解答，谢谢。
                     * 不要编写任何关于手动模式的教程，避免非法传播使用。
                     * 
                     */
                    if (DecryptionHelper.GetMD5(auth) == "295f634af60d61dfa52a5f35849ac42b")
                    {
                        string genHash = DateTime.Now.ToString();
                        string md5 = DecryptionHelper.GetMD5(genHash);
                        UserBakConfig config = new UserBakConfig();
                        config.Hash = md5;
                        string workspacePath = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
                        config.UserWorkspacePath = Path.Combine(workspacePath, md5);

                        WXWorkspace workspace = new WXWorkspace(config);
                        workspace.ManualInit();

                        MessageBox.Show("已经创建空的配置文件，请完善该配置文件后，点击开始解密","提示");
                    }
                }
                else
                {
                    MessageBox.Show("未完成声明文件，请先确认声明", "错误");
                }
            }
            else
            {
                cb_manual.IsChecked = false;
            }
        }
    }
}
