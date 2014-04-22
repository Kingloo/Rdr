using System;
using System.Windows.Data;
using System.Windows.Media;

namespace Rdr
{
    [ValueConversion(typeof(string), typeof(string))]
    class ShortTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is string))
            {
                throw new ArgumentException("Converters.cs -> Convert -> value must be string - and some stuff");
            }

            string title = value as string;
            int maxLength = 27;

            if (title.Length > maxLength)
            {
                title = title.Substring(0, maxLength);
            }

            return title;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value as string;
        }
    }

    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    class FeedUpdatingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;

            if (b)
            {
                return Brushes.Green;
            }
            else
            {
                return Brushes.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

}
