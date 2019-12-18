using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Rdr.Gui.Converters
{
    public abstract class BooleanConverterBase<T> : DependencyObject, IValueConverter
    {
        public T True
        {
            get => (T)GetValue(TrueProperty);
            set => SetValue(TrueProperty, value);
        }

        public T False
        {
            get => (T)GetValue(FalseProperty);
            set => SetValue(FalseProperty, value);
        }

        public static readonly DependencyProperty TrueProperty = DependencyProperty.Register(
            "True",
            typeof(T),
            typeof(BooleanConverterBase<T>),
            new PropertyMetadata(null));

        public static readonly DependencyProperty FalseProperty = DependencyProperty.Register(
            "False",
            typeof(T),
            typeof(BooleanConverterBase<T>),
            new PropertyMetadata(null));

        [return: MaybeNull]
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (bool)value ? True : False;

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException($"converting from {value.GetType().ToString()} to {targetType.ToString()} is not implemented!");
    }
}
