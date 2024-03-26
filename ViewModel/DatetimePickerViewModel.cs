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
    public partial class DatetimePickerViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime startDate = DateTime.Now.AddMonths(-1);

        [ObservableProperty]
        private DateTime endDate = DateTime.Now;

        [ObservableProperty]
        private DateTime pickDate = DateTime.Now.AddDays(-1);

        [ObservableProperty]
        private int dateType = 1;
    }

    public class DateTypeConverter : IValueConverter
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
