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
using System.Windows.Navigation;
using System.Windows.Shapes;
using WechatPCMsgBakTool.Model;

namespace WechatPCMsgBakTool.Pages
{
    /// <summary>
    /// Workspace.xaml 的交互逻辑
    /// </summary>
    public partial class Workspace : Page
    {
        public WXUserReader? UserReader { get; set; }
        public Workspace()
        {
            InitializeComponent();
            UserBakConfig? config = Main2.CurrentUserBakConfig;
            if (config != null)
            {
                if (config.Decrypt)
                {
                    btn_decrypt.IsEnabled = false;
                    btn_read.IsEnabled = true;
                }
            }
        }

        private void btn_decrypt_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void btn_read_Click(object sender, RoutedEventArgs e)
        {
            if (Main2.CurrentUserBakConfig == null)
            {
                MessageBox.Show("工作区配置加载失败，请检查配置文件是否正常","错误");
                return;
            }

            UserReader = new WXUserReader(Main2.CurrentUserBakConfig);
        }
    }
}
