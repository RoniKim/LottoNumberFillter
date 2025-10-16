using System;
using System.Globalization;
using System.Windows.Data;

namespace LottoNumber.Converters
{
    public sealed class BooleanToStatusConverter : IValueConverter
    {
        public string BusyText { get; set; } = "작업 중...";
        public string IdleText { get; set; } = "대기 중";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isBusy = value is bool flag && flag;
            return isBusy ? BusyText : IdleText;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
