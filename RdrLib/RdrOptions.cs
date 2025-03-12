using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RdrLib
{
	public class RdrOptions
	{
		public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) Gecko/20100101 Firefox/136.0";
		public const int DefaultUpdateConcurrency = 4;
		public static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromMinutes(15d);

		public string CustomUserAgent { get; init; } = DefaultUserAgent;

		[Required]
		public string FeedsFilePath { get; init; } = string.Empty;

		public string DownloadDirectory { get; init; } = string.Empty;

		public int UpdateConcurrency { get; init; } = DefaultUpdateConcurrency;

		public TimeSpan UpdateInterval { get; init; } = DefaultUpdateInterval;

		public TimeSpan RateLimitOnHttpTimeout { get; init; } = TimeSpan.Zero;

		[JsonConverter(typeof(JsonStringEnumConverter))]
		public RateLimitChangeStrategy RateLimitChangeStrategy { get; init; } = RateLimitChangeStrategy.Double;

		public RdrOptions() { }
	}
}
