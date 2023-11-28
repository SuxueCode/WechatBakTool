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

namespace WechatPCMsgBakTool
{
    /// <summary>
    /// Main2.xaml 的交互逻辑
    /// </summary>
    public partial class Main2 : Window
    {
        public Main2()
        {
            InitializeComponent();
            lab_version.Content += $" {Application.ResourceAssembly.GetName().Version}";
        }

        private void img_btn_close_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
