using System;
using System.Globalization;
using System.Windows.Data;

namespace BBSReader
{
    [ValueConversion(typeof(object), typeof(string))]
    public class SKTypeToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            dynamic d = value;
            return d.SKType.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}