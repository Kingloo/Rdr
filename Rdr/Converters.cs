using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Rdr.Model;

namespace Rdr
{
    public abstract class GenericBooleanConverter<T> : IValueConverter
    {
        public T True { get; set; }
        public T False { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? True : False;

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => default(T);
    }

    [ValueConversion(typeof(bool), typeof(Style))]
    public class FeedUpdatingConverter : GenericBooleanConverter<Style> { }

    [ValueConversion(typeof(bool), typeof(SolidColorBrush))]
    public class BoolToBrushConverter : GenericBooleanConverter<SolidColorBrush> { }


    public class FeedItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate WithEnclosure { get; set; }
        public DataTemplate NoEnclosure { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            RdrFeedItem feedItem = (RdrFeedItem)item;

            return feedItem.HasEnclosure ? WithEnclosure : NoEnclosure;
        }
    }
}
