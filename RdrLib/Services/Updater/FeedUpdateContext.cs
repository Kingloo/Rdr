using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using RdrLib.Model;

namespace RdrLib.Services.Updater
{
	public class FeedUpdateContext
	{
		public Uri Uri { get; }
		/// <summary>
		/// Start timestamp of web request. Updates that do not lead to an actual web request, such as when under a rate limit, do not update this.
		/// </summary>
		public DateTimeOffset Start { get; set; } = DateTimeOffset.MinValue;
		/// <summary>
		/// End timestamp of web request. Updates that do not lead to an actual web request, such as when under a rate limit, do not update this.
		/// </summary>
		public DateTimeOffset Finish { get; set; } = DateTimeOffset.MinValue;
		public HttpStatusCode? StatusCode { get; set; } = null;
		public DateTimeOffset? LastModified { get; set; } = null;
		public EntityTagHeaderValue? ETag { get; set; } = null;
		public TimeSpan RateLimit { get; set; } = TimeSpan.Zero;
		public Exception? Exception { get; set; } = null;
		public RedirectData? RedirectData { get; set; } = null;

		public FeedUpdateContext(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			if (!feed.Link.IsAbsoluteUri)
			{
				throw new ArgumentException($"feed's link was not absolute ('{feed.Link}')", nameof(feed));
			}

			Uri = feed.Link;
		}

		public override string ToString()
		{
			CultureInfo ci = CultureInfo.CurrentCulture;

			return new StringBuilder()
				.AppendLine(Uri.AbsoluteUri)
				.AppendLine(ci, $"Start '{Start}'")
				.AppendLine(Helpers.HttpStatusCodeHelpers.FormatStatusCode(StatusCode))
				.AppendLine(ci, $"LastModified '{LastModified}'")
				.AppendLine(ci, $"ETag {ETag?.ToString() ?? "null"}")
				.AppendLine(ci, $"Rate limit '{RateLimit}'")
				.AppendLine(Exception?.GetType().Name ?? "no exception")
				.Append(ci, $"Finish '{Finish}'")
				.ToString();
		}
	}
}
