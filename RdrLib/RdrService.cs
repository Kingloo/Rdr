using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdrLib.Helpers;
using RdrLib.Model;
using static RdrLib.EventIds.RdrService;
using static RdrLib.RdrServiceLoggerMessages;

namespace RdrLib
{
	public partial class RdrService : IRdrService
	{
		private const string rateLimitRemainingFormat = @"hh\:mm\:ss";
		private const string rateLimitRemainingWithDaysFormat = @"d\.hh\:mm\:ss";

		private readonly ConcurrentDictionary<Uri, ETag2> etags = new ConcurrentDictionary<Uri, ETag2>();
		private readonly ConcurrentDictionary<Uri, RetryHeaderWithTimestamp> updates = new ConcurrentDictionary<Uri, RetryHeaderWithTimestamp>();
		private readonly ConcurrentDictionary<Uri, DateTimeOffset> lastModifiedHeaders = new ConcurrentDictionary<Uri, DateTimeOffset>();

		private readonly ObservableCollection<Feed> feeds = new ObservableCollection<Feed>();
		public IReadOnlyCollection<Feed> Feeds { get => feeds; }

		private readonly IHttpClientFactory httpClientFactory;
		private readonly IOptionsMonitor<RdrOptions> rdrOptionsMonitor;
		private readonly ILogger<RdrService> logger;

		public RdrService(
			IHttpClientFactory httpClientFactory,
			IOptionsMonitor<RdrOptions> rdrOptionsMonitor,
			ILogger<RdrService> logger)
		{
			ArgumentNullException.ThrowIfNull(httpClientFactory);
			ArgumentNullException.ThrowIfNull(rdrOptionsMonitor);
			ArgumentNullException.ThrowIfNull(logger);

			this.httpClientFactory = httpClientFactory;
			this.rdrOptionsMonitor = rdrOptionsMonitor;
			this.logger = logger;
		}

		public bool Add(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			if (!feeds.Contains(feed))
			{
				feeds.Add(feed);

				LogFeedAdded(logger, feed.Link.AbsoluteUri);

				return true;
			}

			return false;
		}

		public int Add(IEnumerable<Feed> feeds)
		{
			ArgumentNullException.ThrowIfNull(feeds);

			int added = 0;

			foreach (Feed feed in feeds)
			{
				if (Add(feed))
				{
					added++;
				}
			}

			return added;
		}

		public bool Remove(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			bool removed = feeds.Remove(feed);

			if (removed)
			{
				LogFeedRemoved(logger, feed.Link.AbsoluteUri);
			}
			else
			{
				LogFeedRemovedFailed(logger, feed.Link.AbsoluteUri);
			}

			return removed;
		}

		public int Remove(IEnumerable<Feed> feeds)
		{
			ArgumentNullException.ThrowIfNull(feeds);

			int removed = 0;

			foreach (Feed feed in feeds)
			{
				if (Remove(feed))
				{
					removed++;
				}
			}

			return removed;
		}

		public void MarkAsRead(Item item)
		{
			ArgumentNullException.ThrowIfNull(item);

			item.Unread = false;

			LogMarkAsRead(logger, item.FeedName, item.Name);
		}

		public void MarkAsRead(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			foreach (Item each in feed.Items)
			{
				MarkAsRead(each);
			}
		}

		public void MarkAllAsRead()
		{
			foreach (Feed feed in feeds)
			{
				foreach (Item item in feed.Items)
				{
					MarkAsRead(item);
				}
			}

			LogMarkAllAsRead(logger);
		}

		public void ClearFeeds()
		{
			feeds.Clear();

			LogClearFeeds(logger);
		}

		public Task UpdateAsync(Feed feed)
			=> UpdateAsyncInternal(feed, CancellationToken.None);

		public Task UpdateAsync(Feed feed, CancellationToken cancellationToken)
			=> UpdateAsyncInternal(feed, cancellationToken);

		private async Task UpdateAsyncInternal(Feed feed, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(feed);

			await UpdateFeedAsync(feed, cancellationToken).ConfigureAwait(false);
		}

		public Task UpdateAsync(IEnumerable<Feed> feeds)
			=> UpdateAsyncInternal(feeds, BatchOptions.Default, CancellationToken.None);

		public Task UpdateAsync(IEnumerable<Feed> feeds, BatchOptions batchOptions)
			=> UpdateAsyncInternal(feeds, batchOptions, CancellationToken.None);

		public Task UpdateAsync(IEnumerable<Feed> feeds, CancellationToken cancellationToken)
			=> UpdateAsyncInternal(feeds, BatchOptions.Default, cancellationToken);

		public Task UpdateAsync(IEnumerable<Feed> feeds, BatchOptions batchOptions, CancellationToken cancellationToken)
			=> UpdateAsyncInternal(feeds, batchOptions, cancellationToken);

		private Task UpdateAsyncInternal(IEnumerable<Feed> feeds, BatchOptions batchOptions, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(feeds);
			ArgumentNullException.ThrowIfNull(batchOptions);

			RdrOptions currentRdrOptions = rdrOptionsMonitor.CurrentValue;

			ParallelOptions everythingElseParallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = currentRdrOptions.UpdateConcurrency
			};

			(List<List<Feed>> largeGroups, List<Feed> everythingElse) = GetFeedGroups(feeds, batchOptions.BatchWhenLargerThan);

			List<Task> tasks = new List<Task>();

			foreach (List<Feed> largeGroup in largeGroups)
			{
				Task task = Task.Run(
					async () => await UpdateBatchedFeedAsync(largeGroup, currentRdrOptions, batchOptions, cancellationToken).ConfigureAwait(true),
					cancellationToken);

				tasks.Add(task);
			}

			Task everythingElseTask = Task.Run(
				async () => await Parallel.ForEachAsync(everythingElse, everythingElseParallelOptions, UpdateFeedAsync).ConfigureAwait(true),
				cancellationToken);

			tasks.Add(everythingElseTask);

			return Task.WhenAll(tasks);
		}

		private async Task UpdateBatchedFeedAsync(List<Feed> largeGroup, RdrOptions rdrOptions, BatchOptions batchOptions, CancellationToken cancellationToken)
		{
			int countTaken = 0;

			ParallelOptions batchedParallelOptions = new ParallelOptions
			{
				MaxDegreeOfParallelism = rdrOptions.UpdateConcurrency,
				CancellationToken = cancellationToken
			};

			foreach (Feed[] chunk in largeGroup.Chunk(batchOptions.ChunkSize))
			{
				countTaken += chunk.Length;

				await Parallel.ForEachAsync(chunk, batchedParallelOptions, UpdateFeedAsync).ConfigureAwait(true);

				if (countTaken < largeGroup.Count)
				{
					await Task.Delay(batchOptions.Interval, cancellationToken).ConfigureAwait(true);
				}
			}
		}

		private async ValueTask UpdateFeedAsync(Feed feed, CancellationToken cancellationToken)
		{
			LogFeedUpdateStarted(logger, feed.Name, feed.Link.AbsoluteUri);

			RdrOptions currentRdrOptions = rdrOptionsMonitor.CurrentValue;

			DateTimeOffset now = DateTimeOffset.Now;

			feed.Status = FeedStatus.Updating;

			TimeSpan rateLimitTimeRemaining = IsFeedUnderRateLimit(feed, now);

			if (rateLimitTimeRemaining > TimeSpan.Zero) // feed is under rate limit conditions
			{
				LogExistingRateLimit(logger, feed.Name, feed.Link.AbsoluteUri, FormatTimeSpan(rateLimitTimeRemaining));

				feed.Status = FeedStatus.RateLimited;

				return;
			}

			string responseText = string.Empty;

			using (HttpClient client = httpClientFactory.CreateClient("RdrService"))
			{
				bool shouldProceedWithBody = false;

				try
				{
					using ResponseSet responseSet = await Web2.PerformHeaderRequest(client, feed.Link, cancellationToken).ConfigureAwait(false);

					HttpResponseMessage lastResponse = responseSet.Responses.Last().Response;

					bool areETagsDifferent = AreETagsDifferent(lastResponse, feed);
					bool isLastModifiedHeaderDifferent = IsLastModifiedHeaderDifferent(lastResponse, feed);
					bool responseContainsNoRateLimitHeader = ResponseContainsNoRateLimitHeader(lastResponse, feed, now, currentRdrOptions);

					shouldProceedWithBody = areETagsDifferent
						&& isLastModifiedHeaderDifferent
						&& responseContainsNoRateLimitHeader;

					if (shouldProceedWithBody)
					{
						updates.AddOrUpdate(
							feed.Link,
							(Uri _) => new RetryHeaderWithTimestamp(now, null),
							(Uri _, RetryHeaderWithTimestamp _) => new RetryHeaderWithTimestamp(now, null)
						);

						responseText = await Web2.PerformBodyRequestToString(lastResponse, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						if (feed.Status == FeedStatus.Updating)
						{
							feed.Status = FeedStatus.Ok;
						}

						return;
					}
				}
				catch (OperationCanceledException ex) when (ex.InnerException is TimeoutException te)
				{
					TimeSpan timeoutRetry = updates.TryGetValue(feed.Link, out RetryHeaderWithTimestamp? retryHeaderWithTimestamp)
						&& retryHeaderWithTimestamp.RetryHeader is RetryConditionHeaderValue retryConditionHeaderValue
							? ApplyRateLimitChangeStrategy(retryConditionHeaderValue, now, currentRdrOptions.RateLimitChangeStrategy)
							: currentRdrOptions.RateLimitOnHttpTimeout;

					LogTimeout(logger, feed.Name, feed.Link.AbsoluteUri, FormatTimeSpan(timeoutRetry));

					RetryHeaderWithTimestamp timeoutRetryHeader = new RetryHeaderWithTimestamp(now, new RetryConditionHeaderValue(timeoutRetry));

					updates.AddOrUpdate(
						feed.Link,
						(Uri _) => timeoutRetryHeader,
						(Uri _, RetryHeaderWithTimestamp _) => timeoutRetryHeader
					);

					feed.Status = FeedStatus.Timeout;

					return;
				}
				catch (HttpIOException)
				{
					feed.Status = FeedStatus.InternetError;

					return;
				}
				catch (HttpRequestException)
				{
					feed.Status = FeedStatus.InternetError;

					return;
				}
			}

			if (!XmlHelpers.TryParse(responseText, out XDocument? document))
			{
				feed.Status = FeedStatus.ParseFailed;
				return;
			}

			feed.Name = FeedHelpers.GetName(document);

			IReadOnlyCollection<Item> items = FeedHelpers.GetItems(document, feed.Name);

			feed.AddMany(items);

			feed.Status = FeedStatus.Ok;

			LogFeedUpdateSucceeded(logger, feed.Name, feed.Link.AbsoluteUri);
		}

		private static (List<List<Feed>>, List<Feed>) GetFeedGroups(IEnumerable<Feed> allFeeds, int batchWhenLargerThan)
		{
			List<IGrouping<string, Feed>> groups = allFeeds.GroupBy(static feed => feed.Link.DnsSafeHost).ToList();

			List<List<Feed>> largeGroups = groups
				.Where(group => group.Count() > batchWhenLargerThan)
				.Select(static group => new List<Feed>(group))
				.ToList();

			List<Feed> everythingElse = groups
				.Where(group => group.Count() <= batchWhenLargerThan)
				.SelectMany(static group => group)
				.ToList();

			return (largeGroups, everythingElse);
		}

		private TimeSpan IsFeedUnderRateLimit(Feed feed, DateTimeOffset thisUpdate)
		{
			if (updates.TryGetValue(feed.Link, out RetryHeaderWithTimestamp? headerWithTimestamp))
			{
				(DateTimeOffset previousUpdate, RetryConditionHeaderValue? retryHeader) = headerWithTimestamp;

				if (retryHeader is RetryConditionHeaderValue retryHeaderValue)
				{
					return Web2.GetAmountOfTimeLeftOnRateLimit(retryHeader, thisUpdate, previousUpdate);
				}
			}

			return TimeSpan.Zero;
		}

		private bool AreETagsDifferent(HttpResponseMessage response, Feed feed)
		{
			if (etags.TryGetValue(feed.Link, out ETag2? previousETag))
			{
				if (Web2.HasETagMatch(response, previousETag))
				{
					LogETagMatch(logger, feed.Name, feed.Link.AbsoluteUri);

					feed.Status = FeedStatus.Ok;

					return false;
				}
			}

			if (response.Headers.ETag?.Tag is string currentETag)
			{
				etags.AddOrUpdate(feed.Link, (_) => new ETag2(currentETag), (_, _) => new ETag2(currentETag));
			}

			return true;
		}

		private bool IsLastModifiedHeaderDifferent(HttpResponseMessage response, Feed feed)
		{
			if (lastModifiedHeaders.TryGetValue(feed.Link, out DateTimeOffset previousLastModified))
			{
				if (response.Content.Headers.LastModified is DateTimeOffset newLastModified)
				{
					if (previousLastModified == newLastModified)
					{
						LogLastModifiedUnchanged(logger, feed.Name, feed.Link.AbsoluteUri, previousLastModified.ToString(CultureInfo.CurrentCulture));

						feed.Status = FeedStatus.Ok;

						return false;
					}
				}
			}

			if (response.Content.Headers.LastModified is DateTimeOffset lastModified)
			{
				lastModifiedHeaders.AddOrUpdate(feed.Link, (_) => lastModified, (_, _) => lastModified);
			}

			return true;
		}

		private bool ResponseContainsNoRateLimitHeader(HttpResponseMessage response, Feed feed, DateTimeOffset now, RdrOptions rdrOptions)
		{
			if (response.Headers.RetryAfter is RetryConditionHeaderValue newRetryHeader)
			{
				// if we had an existing rate limit header and we got another on this request
				// make the rate limit even longer than the new header requires

				TimeSpan timeLeft = updates.TryGetValue(feed.Link, out RetryHeaderWithTimestamp? existingRetryHeaderWithTimestamp)
					&& existingRetryHeaderWithTimestamp.RetryHeader is RetryConditionHeaderValue existingRetryHeaderValue
						? ApplyRateLimitChangeStrategy(existingRetryHeaderValue, now, rdrOptions.RateLimitChangeStrategy)
						: Web2.GetAmountOfTimeLeftOnRateLimit(newRetryHeader, now);

				updates.AddOrUpdate( // replacing with new header with (potentially adjusted) backoff time
					feed.Link,
					(Uri _) => new RetryHeaderWithTimestamp(now, new RetryConditionHeaderValue(timeLeft)),
					(Uri _, RetryHeaderWithTimestamp _) => new RetryHeaderWithTimestamp(now, new RetryConditionHeaderValue(timeLeft))
				);

				LogNewRateLimit(logger, feed.Name, feed.Link.AbsoluteUri, FormatTimeSpan(timeLeft));

				feed.Status = FeedStatus.RateLimited;

				return false;
			}

			return true;
		}

		private static TimeSpan ApplyRateLimitChangeStrategy(RetryConditionHeaderValue existingRetryHeaderValue, DateTimeOffset now, RateLimitChangeStrategy rateLimitChangeStrategy)
		{
			TimeSpan existingHeaderRateLimit = Web2.GetAmountOfTimeLeftOnRateLimit(existingRetryHeaderValue, now);

			return rateLimitChangeStrategy switch
			{
				RateLimitChangeStrategy.Double => existingHeaderRateLimit * 2,
				RateLimitChangeStrategy.Triple => existingHeaderRateLimit * 3,
				RateLimitChangeStrategy.AddHour => existingHeaderRateLimit.Add(TimeSpan.FromHours(1d)),
				RateLimitChangeStrategy.AddDay => existingHeaderRateLimit.Add(TimeSpan.FromDays(1d)),
				_ => throw new ArgumentException($"invalid value: '{rateLimitChangeStrategy}'", nameof(rateLimitChangeStrategy))
			};
		}

		private static string FormatTimeSpan(TimeSpan ts)
		{
			return ts.TotalSeconds >= 86400
				? ts.ToString(rateLimitRemainingWithDaysFormat, CultureInfo.CurrentCulture)
				: ts.ToString(rateLimitRemainingFormat, CultureInfo.CurrentCulture);
		}

		[System.Diagnostics.DebuggerStepThrough]
		public Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file)
			=> DownloadEnclosureAsyncInternal(enclosure, file, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progress)
			=> DownloadEnclosureAsyncInternal(enclosure, file, progress, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, file, null, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progress, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, file, progress, cancellationToken);

		private async Task<long> DownloadEnclosureAsyncInternal(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress>? progress, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(enclosure);
			ArgumentNullException.ThrowIfNull(file);

			using HttpClient client = httpClientFactory.CreateClient();

			using ResponseSet responseSet = await Web2.PerformHeaderRequest(client, enclosure.Link, cancellationToken).ConfigureAwait(false);

			HttpResponseMessage lastResponse = responseSet.Responses.Last().Response;

			Task<long> downloadEnclosureTask = progress is not null
				? Web2.PerformBodyRequestToFile(lastResponse, file, progress, cancellationToken)
				: Web2.PerformBodyRequestToFile(lastResponse, file, cancellationToken);

			return await downloadEnclosureTask.ConfigureAwait(false);
		}
	}

	internal static partial class RdrServiceLoggerMessages
	{
		// https://andrewlock.net/exploring-dotnet-6-part-8-improving-logging-performance-with-source-generators/

		[LoggerMessage(FeedAddedId, LogLevel.Debug, "feed added '{Link}'")]
		internal static partial void LogFeedAdded(ILogger<RdrService> logger, string link);

		[LoggerMessage(FeedRemovedId, LogLevel.Debug, "feed removed '{Link}'")]
		internal static partial void LogFeedRemoved(ILogger<RdrService> logger, string link);

		[LoggerMessage(FeedRemovedFailedId, LogLevel.Debug, "failed to remove feed '{Link}'")]
		internal static partial void LogFeedRemovedFailed(ILogger<RdrService> logger, string link);

		[LoggerMessage(FeedUpdateStartedId, LogLevel.Debug, "updating {FeedName} ({FeedLink})")]
		internal static partial void LogFeedUpdateStarted(ILogger<RdrService> logger, string feedName, string feedLink);

		[LoggerMessage(FeedUpdateFailedId, LogLevel.Warning, "{Error} for '{Name}'")]
		internal static partial void LogFeedUpdateFailed(ILogger<RdrService> logger, string error, string name);

		[LoggerMessage(FeedUpdateSucceededId, LogLevel.Debug, "updated '{FeedName}' ({FeedLink})")]
		internal static partial void LogFeedUpdateSucceeded(ILogger<RdrService> logger, string feedName, string feedLink);

		[LoggerMessage(ETagMatchId, LogLevel.Debug, "etag match for '{FeedName}' ('{FeedLink}')")]
		internal static partial void LogETagMatch(ILogger<RdrService> logger, string feedName, string feedLink);

		[LoggerMessage(TimeoutId, LogLevel.Warning, "timeout for '{FeedName}' ('{FeedLink}') - will retry in {RetryTimeout}")]
		internal static partial void LogTimeout(ILogger<RdrService> logger, string feedName, string feedLink, string retryTimeout);

		[LoggerMessage(MarkAsReadId, LogLevel.Trace, "marked item as read: '{FeedName}'->'{ItemName}'")]
		internal static partial void LogMarkAsRead(ILogger<RdrService> logger, string feedName, string itemName);

		[LoggerMessage(MarkAllAsReadId, LogLevel.Debug, "marked ALL as read")]
		internal static partial void LogMarkAllAsRead(ILogger<RdrService> logger);

		[LoggerMessage(ClearFeedsId, LogLevel.Debug, "cleared ALL feeds")]
		internal static partial void LogClearFeeds(ILogger<RdrService> logger);

		[LoggerMessage(NewRateLimitId, LogLevel.Warning, "new rate limit - '{FeedName}' ('{FeedLink}') - {TimeRemaining} remaining")]
		internal static partial void LogNewRateLimit(ILogger<RdrService> logger, string feedName, string feedLink, string timeRemaining);

		[LoggerMessage(ExistingRateLimitId, LogLevel.Debug, "update skipped under existing rate limit - '{FeedName}' ('{FeedLink}') - {timeRemaining} remaining")]
		internal static partial void LogExistingRateLimit(ILogger<RdrService> logger, string feedName, string feedLink, string timeRemaining);

		[LoggerMessage(LastModifiedUnchangedId, LogLevel.Debug, "LastModified unchanged - '{FeedName}' ('{FeedLink}') - {LastModified}")]
		internal static partial void LogLastModifiedUnchanged(ILogger<RdrService> logger, string feedName, string feedLink, string lastModified);
	}
}
