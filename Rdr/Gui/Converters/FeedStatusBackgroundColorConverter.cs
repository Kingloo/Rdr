using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RdrLib.Model;

namespace Rdr.Gui.Converters
{
	[ValueConversion(typeof(FeedStatus), typeof(Brush))]
	public class FeedStatusBackgroundColorConverter : IValueConverter
	{
		private static readonly Brush defaultBackgroundColor = Brushes.White;

		public Brush None { get; set; } = defaultBackgroundColor;
		public Brush Ok { get; set; } = defaultBackgroundColor;
		public Brush Updating { get; set; } = defaultBackgroundColor;
		public Brush DoesNotExist { get; set; } = defaultBackgroundColor;
		public Brush Forbidden { get; set; } = defaultBackgroundColor;
		public Brush ParseFailed { get; set; } = defaultBackgroundColor;
		public Brush MovedCannotFollow { get; set; } = defaultBackgroundColor;
		public Brush Timeout { get; set; } = defaultBackgroundColor;
		public Brush Dns { get; set; } = defaultBackgroundColor;
		public Brush RateLimited { get; set; } = defaultBackgroundColor;
		public Brush InternetError { get; set; } = defaultBackgroundColor;
		public Brush CertificateRevocationCheckFailed { get; set; } = defaultBackgroundColor;
		public Brush Broken { get; set; } = defaultBackgroundColor;
		public Brush Other { get; set; } = defaultBackgroundColor;

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
				FeedStatus.Timeout => Timeout,
				FeedStatus.Dns => Dns,
				FeedStatus.RateLimited => RateLimited,
				FeedStatus.MovedCannotFollow => MovedCannotFollow,
				FeedStatus.InternetError => InternetError,
				FeedStatus.CertificateRevocationCheckFailed => CertificateRevocationCheckFailed,
				FeedStatus.Broken => Broken,
				FeedStatus.Other => Other,
				_ => throw new ArgumentException($"unknown FeedStatus '{value}'", nameof(value))
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("converting from color to FeedStatus is not supported!");
		}
	}
}
