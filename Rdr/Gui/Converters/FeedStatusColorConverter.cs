using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RdrLib.Model;

namespace Rdr.Gui.Converters
{
	[ValueConversion(typeof(FeedStatus), typeof(Brush))]
	public class FeedStatusColorConverter : IValueConverter
	{
		private static readonly Brush defaultColor = Brushes.White;

		public Brush None { get; set; } = defaultColor;
		public Brush Ok { get; set; } = defaultColor;
		public Brush Updating { get; set; } = defaultColor;
		public Brush DoesNotExist { get; set; } = defaultColor;
		public Brush Forbidden { get; set; } = defaultColor;
		public Brush ParseFailed { get; set; } = defaultColor;
		public Brush MovedCannotFollow { get; set; } = defaultColor;
		public Brush Timeout { get; set; } = defaultColor;
		public Brush Dns { get; set; } = defaultColor;
		public Brush RateLimited { get; set; } = defaultColor;
		public Brush InternetError { get; set; } = defaultColor;
		public Brush ConnectionError { get; set; } = defaultColor;
		public Brush Broken { get; set; } = defaultColor;
		public Brush Other { get; set; } = defaultColor;

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
				FeedStatus.ConnectionError => ConnectionError,
				FeedStatus.Broken => Broken,
				FeedStatus.Other => Other,
				_ => throw new ArgumentException($"unknown FeedStatus '{value}'", nameof(value))
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("converting from Brush to FeedStatus is not supported!");
		}
	}
}
