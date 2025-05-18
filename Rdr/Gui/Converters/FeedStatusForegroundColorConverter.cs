using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RdrLib.Model;

namespace Rdr.Gui.Converters
{
	[ValueConversion(typeof(FeedStatus), typeof(Brush))]
	public class FeedStatusForegroundColorConverter : IValueConverter
	{
		private static readonly Brush defaultForegroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF7A7A7A"));

		public Brush None { get; set; } = defaultForegroundColor;
		public Brush Ok { get; set; } = defaultForegroundColor;
		public Brush Updating { get; set; } = defaultForegroundColor;
		public Brush DoesNotExist { get; set; } = defaultForegroundColor;
		public Brush Forbidden { get; set; } = defaultForegroundColor;
		public Brush ParseFailed { get; set; } = defaultForegroundColor;
		public Brush MovedCannotFollow { get; set; } = defaultForegroundColor;
		public Brush Timeout { get; set; } = defaultForegroundColor;
		public Brush Dns { get; set; } = defaultForegroundColor;
		public Brush RateLimited { get; set; } = defaultForegroundColor;
		public Brush InternetError { get; set; } = defaultForegroundColor;
		public Brush CertificateRevocationCheckFailed { get; set; } = defaultForegroundColor;
		public Brush Broken { get; set; } = defaultForegroundColor;
		public Brush Other { get; set; } = defaultForegroundColor;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			ArgumentNullException.ThrowIfNull(value);

			if (value.GetType() != typeof(FeedStatus))
			{
				throw new ArgumentException(
					$"value type should be '{typeof(FeedStatus).FullName}', was '{value.GetType().FullName ?? "null"}'",
					nameof(targetType));
			}

			if (targetType != typeof(Brush))
			{
				throw new ArgumentException(
					$"target type should be '{typeof(Brush).FullName}', was '{targetType?.FullName ?? "null"}'",
					nameof(targetType));
			}

			return (FeedStatus)value switch
			{
				FeedStatus.None => None,
				FeedStatus.Ok => Ok,
				FeedStatus.Updating => Updating,
				FeedStatus.DoesNotExist => DoesNotExist,
				FeedStatus.Forbidden => Forbidden,
				FeedStatus.ParseFailed => ParseFailed,
				FeedStatus.MovedCannotFollow => MovedCannotFollow,
				FeedStatus.Timeout => Timeout,
				FeedStatus.Dns => Dns,
				FeedStatus.RateLimited => RateLimited,
				FeedStatus.InternetError => InternetError,
				FeedStatus.CertificateRevocationCheckFailed => CertificateRevocationCheckFailed,
				FeedStatus.Broken => Broken,
				FeedStatus.Other => Other,
				_ => throw new ArgumentException($"unknown FeedStatus '{value}'", nameof(value))
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("converting from Brush to FeedStatus is not supported");
		}
	}
}
