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
using System.Windows.Shapes;
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

        }
    }
}
