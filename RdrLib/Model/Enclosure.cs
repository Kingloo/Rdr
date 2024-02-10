using System;
using System.Globalization;
using System.Text;

namespace RdrLib.Model
{
	public class Enclosure : BindableBase
	{
		public string FeedName { get; set; } = string.Empty;
		public Uri Link { get; }
		public Int64? Size { get; } = null;

		private bool _isDownloading = false;
		public bool IsDownloading
		{
			get => _isDownloading;
			set => SetProperty(ref _isDownloading, value, nameof(IsDownloading));
		}

		private string _message = "Download";
		public string Message
		{
			get => _message;
			set => SetProperty(ref _message, value, nameof(Message));
		}

		public Enclosure(Uri uri, Int64? size)
		{
			ArgumentNullException.ThrowIfNull(uri);

			Link = uri;
			Size = size;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(FeedName);
			sb.AppendLine(Link?.AbsoluteUri ?? "no link");
			sb.AppendLine(CultureInfo.CurrentCulture, $"size: {Size}");

			return sb.ToString();
		}
	}
}
