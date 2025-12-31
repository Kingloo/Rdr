using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RdrLib
{
	public class RdrOptions
	{
		public const string DefaultUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:146.0) Gecko/20100101 Firefox/146.0";
		public const int DefaultUpdateConcurrency = 4;
		public static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromMinutes(15d);
		public static readonly TimeSpan DefaultBatchUpdateDelay = TimeSpan.FromSeconds(0.5d);
		public const int DefaultRandomiseTake = 10;

		public string CustomUserAgent { get; init; } = DefaultUserAgent;

		[Required]
		public string FeedsFilePath { get; init; } = string.Empty;

		public string DownloadDirectory { get; init; } = string.Empty;

		public int UpdateConcurrency { get; init; } = DefaultUpdateConcurrency;

		public TimeSpan UpdateInterval { get; init; } = DefaultUpdateInterval;

		public TimeSpan BatchUpdateDelay { get; init; } = DefaultBatchUpdateDelay;

		public bool Randomise { get; init; } = false;

		public int RandomiseTake { get; init; } = DefaultRandomiseTake;

		public TimeSpan RateLimitOnHttpTimeout { get; init; } = TimeSpan.FromHours(1d);

#pragma warning disable CA1002 // Do not expose generic lists - System.Text.Json cannot deserialize to IList<T>
		public List<string> SkipCrlCheckFor { get; init; } = new List<string>();
#pragma warning restore CA1002 // Do not expose generic lists

		public RdrOptions() { }

		public static readonly RdrOptions Default = new RdrOptions();
	}
}
