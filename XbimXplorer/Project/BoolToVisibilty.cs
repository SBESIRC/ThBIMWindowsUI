using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace XbimXplorer
{
    class BoolToVisibilty : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool s = (bool)value;
            if (null != parameter)
            {
                var str = parameter.ToString();
                if (str == "1")
                    s = !s;
            }
            return s ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var visibility = (Visibility)value;
            var isTrue = visibility != Visibility.Visible;
            if (null != parameter)
            {
                var str = parameter.ToString();
                if (str == "1")
                    isTrue = !isTrue;
            }
            return isTrue;
        }
    }
}
