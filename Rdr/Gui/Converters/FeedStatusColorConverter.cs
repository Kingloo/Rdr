using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using RdrLib;

namespace Rdr.Gui.Converters
{
	[ValueConversion(typeof(FeedStatus), typeof(Brush))]
	public class FeedStatusColorConverter : IValueConverter
	{
		public Brush None { get; set; } = Brushes.White;
		public Brush Ok { get; set; } = Brushes.White;
		public Brush Updating { get; set; } = Brushes.White;
		public Brush DoesNotExist { get; set; } = Brushes.White;
		public Brush Forbidden { get; set; } = Brushes.White;
		public Brush ParseFailed { get; set; } = Brushes.White;
		public Brush MovedCannotFollow { get; set; } = Brushes.White;
		public Brush Timeout { get; set; } = Brushes.White;
		public Brush OtherInternetError { get; set; } = Brushes.White;
		public Brush Broken { get; set; } = Brushes.White;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> (FeedStatus)value switch
			{
				FeedStatus.None => None,
				FeedStatus.Ok => Ok,
				FeedStatus.Updating => Updating,
				FeedStatus.DoesNotExist => DoesNotExist,
				FeedStatus.Forbidden => Forbidden,
				FeedStatus.ParseFailed => ParseFailed,
				FeedStatus.Timeout => Timeout,
				FeedStatus.MovedCannotFollow => MovedCannotFollow,
				FeedStatus.OtherInternetError => OtherInternetError,
				FeedStatus.Broken => Broken,
				_ => None
			};

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException("converting from color to FeedStatus is not supported!");
		}
	}
}
