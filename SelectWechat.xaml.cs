using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using WechatPCMsgBakTool.Helpers;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool
{
    /// <summary>
    /// SelectWechat.xaml 的交互逻辑
    /// </summary>
    public partial class SelectWechat : Window
    {
        List<ProcessInfo> processInfos = new List<ProcessInfo>();
        public ProcessInfo? SelectProcess { get; set; } = null;
        public SelectWechat()
        {
            InitializeComponent();
            //GetWechatProcess();
            GetWechatProcessInfos();
            list_process.ItemsSource = processInfos;
        }

        private void GetWechatProcessInfos()
        {
            processInfos.Clear();
            Process[] processes = Process.GetProcessesByName("wechat");
            foreach (Process p in processes)
            {
                var h_list = ProcessHelper.GetHandles(p);
                foreach (var h in h_list)
                {
                    string name = ProcessHelper.FindHandleName(h, p);
                    if (name != "")
                    {
                        // 预留handle log
                        if (File.Exists("handle.log"))
                        {
                            File.AppendAllText("handle.log", string.Format("{0}|{1}|{2}|{3}\n", p.Id, h.ObjectType, h.Handle, name));
                        }
                        if (name.Contains("\\MicroMsg.db") && name.Substring(name.Length - 3, 3) == ".db")
                        {
                            ProcessInfo info = new ProcessInfo();
                            info.ProcessId = p.Id.ToString();
                            info.ProcessName = p.ProcessName;
                            info.DBPath = DevicePathMapper.FromDevicePath(name);
                            processInfos.Add(info);
                        }
                    }
                }
            }
        }

        public void GetWechatProcess()
        {
            Process p = new Process();
            p.StartInfo.FileName = "tools/handle64.exe";
            p.StartInfo.Arguments = "-p wechat.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();

            string i = p.StandardOutput.ReadToEnd();
            if (i.Contains("SYSINTERNALS SOFTWARE LICENSE TERMS"))
            {
                MessageBox.Show("请先同意Handle64的使用协议，同意后关闭弹窗重新打开新增工作区即可");
                Process p1 = new Process();
                p1.StartInfo.FileName = "tools/handle64.exe";
                p1.StartInfo.Arguments = "-p wechat.exe";
                p1.Start();
            }

            string[] lines = i.Split(new string[] { "\r\n" }, StringSplitOptions.None);
            bool hitFind = false;
            ProcessInfo processInfo = new ProcessInfo();
            foreach (string line in lines)
            {
                if (line.Length < 6)
                    continue;

                if (line.Substring(0, 6).ToLower() == "wechat")
                {
                    hitFind = true;
                    processInfo = new ProcessInfo();
                    string[] lineInfo = line.Split(' ');
                    processInfo.ProcessName = lineInfo[0];
                    processInfo.ProcessId = lineInfo[2];
                }
                if (hitFind)
                {
                    if (line.Substring(line.Length - 11, 11) == "MicroMsg.db")
                    {
                        Regex regex = new Regex("[a-zA-Z]:\\\\([a-zA-Z0-9() ]*\\\\)*\\w*.*\\w*");
                        string path = regex.Match(line).Value;
                        processInfo.DBPath = path;
                        processInfos.Add(processInfo);
                        hitFind = false;
                    }
                }
            }

            list_process.ItemsSource = processInfos;
        }

        private void list_process_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectProcess = list_process.SelectedItem as ProcessInfo;
            if(SelectProcess != null)
            {
                string[] name_raw = SelectProcess.DBPath.Split("\\");
                txt_username.Text = name_raw[name_raw.Length - 3];
                
            }
            
        }

        private void btn_close_Click(object sender, RoutedEventArgs e)
        {
            if (SelectProcess != null)
            {
                SelectProcess.Account = txt_username.Text;
            }
            Close();
        }
    }
}
