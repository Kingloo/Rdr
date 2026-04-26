using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace RdrLib
{
	public class RdrOptions
	{
		public const int DefaultUpdateConcurrency = 4;
		public static readonly TimeSpan DefaultUpdateInterval = TimeSpan.FromMinutes(15d);
		public static readonly TimeSpan DefaultBatchUpdateDelay = TimeSpan.FromSeconds(0.5d);
		public const int DefaultRandomiseTake = 10;

		public string DefaultUserAgent { get; init; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:150.0) Gecko/20100101 Firefox/150.0";
		
		public Collection<CustomUserAgent> DomainSpecificUserAgents { get; init; } = new Collection<CustomUserAgent>();

		[Required]
		public string FeedsFilePath { get; init; } = string.Empty;

		public string DownloadDirectory { get; init; } = string.Empty;

		public int UpdateConcurrency { get; init; } = DefaultUpdateConcurrency;

		public TimeSpan UpdateInterval { get; init; } = DefaultUpdateInterval;

		public TimeSpan BatchUpdateDelay { get; init; } = DefaultBatchUpdateDelay;

		public bool ShouldUpdateRandomSubsetPerDomain { get; init; } = false;

		public int RandomisePerDomainWhenMoreThan { get; init; } = DefaultRandomiseTake;

		public TimeSpan RateLimitOnHttpTimeout { get; init; } = TimeSpan.FromHours(1d);

		public Collection<string> SkipCrlCheckFor { get; init; } = new Collection<string>();

		public RdrOptions() { }

		public static readonly RdrOptions Default = new RdrOptions();
	}
}
