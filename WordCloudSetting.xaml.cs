using System;
using System.Collections.Generic;
using System.Drawing.Text;
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

namespace WechatBakTool
{
    /// <summary>
    /// WordCloudSetting.xaml 的交互逻辑
    /// </summary>
    public partial class WordCloudSetting : Window
    {
        public WordCloudSetting(WordCloudSettingViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            viewModel.FontList = LoadFont();
        }

        private List<string> LoadFont()
        {
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();
            var fontFamilies = installedFontCollection.Families;
            List<string> list = new List<string>();
            foreach ( var fontFamily in fontFamilies )
            {
                list.Add(fontFamily.Name);
            }
            return list;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
