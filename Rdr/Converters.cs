using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Rdr
{
    [ValueConversion(typeof(string), typeof(string))]
    public class ShortTitleConverter : IValueConverter
    {
        private int _maxLength = Int32.MaxValue - 1;
        public int MaxLength
        {
            get { return this._maxLength; }
            set { this._maxLength = value; }
        }
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string title = (string)value;

            if (title.Length > MaxLength)
            {
                title = title.Substring(0, MaxLength);
            }

            return title;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (string)value;
        }
    }

    [ValueConversion(typeof(bool), typeof(Style))]
    public class FeedUpdatingConverter : IValueConverter
    {
        private Style _notUpdating = null;
        public Style NotUpdating
        {
            get { return this._notUpdating; }
            set { this._notUpdating = value; }
        }

        private Style _updating = null;
        public Style Updating
        {
            get { return this._updating; }
            set { this._updating = value; }
        }
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;

            if (b)
            {
                return this.Updating;
            }
            else
            {
                return this.NotUpdating;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return true;
        }
    }

    [ValueConversion(typeof(bool), typeof(Brush))]
    public class UnreadToBackgroundConverter : IValueConverter
    {
        public SolidColorBrush True { get; set; }
        public SolidColorBrush False { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool b = (bool)value;

            if (b)
            {
                return this.True;
            }
            else
            {
                return this.False;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return true;
        }
    }
}
