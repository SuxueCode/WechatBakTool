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
using WechatBakTool.ViewModel;

namespace WechatBakTool.Dialog
{
    /// <summary>
    /// MsgDatetimePicker.xaml 的交互逻辑
    /// </summary>
    public partial class MsgDatetimePicker : Window
    {

        public MsgDatetimePicker(DatetimePickerViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
