using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WechatBakTool.Model;

namespace WechatBakTool.ViewModel
{
    partial class CreateWorkViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<ProcessInfo> processInfos = new List<ProcessInfo>();

        [ObservableProperty]
        private ProcessInfo? selectProcess;

        [ObservableProperty]
        private string userName = "";

        [ObservableProperty]
        private int keyType = -1;
    }

    public class GetKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (int.Parse(parameter.ToString()!) == (int)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (bool)value ? parameter : Binding.DoNothing;
        }
    }
}
