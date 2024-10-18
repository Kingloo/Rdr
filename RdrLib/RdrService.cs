using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdrLib.Exceptions;
using RdrLib.Helpers;
using RdrLib.Model;
using static RdrLib.EventIds.RdrService;
using static RdrLib.Helpers.HttpStatusCodeHelpers;
using static RdrLib.RdrServiceLoggerMessages;

namespace RdrLib
{
	public partial class RdrService : IRdrService
	{
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

			ParallelOptions everythingElseParallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = rdrOptionsMonitor.CurrentValue.UpdateConcurrency
			};

			(List<List<Feed>> largeGroups, List<Feed> everythingElse) = GetFeedGroups(feeds, batchOptions.BatchWhenLargerThan);

			List<Task> tasks = new List<Task>();

			foreach (List<Feed> largeGroup in largeGroups)
			{
				Task task = Task.Run(
					async () => await UpdateBatchedFeedAsync(largeGroup, batchOptions, cancellationToken).ConfigureAwait(true),
					cancellationToken);

				tasks.Add(task);
			}

			Task everythingElseTask = Task.Run(
				async () => await Parallel.ForEachAsync(everythingElse, everythingElseParallelOptions, UpdateFeedAsync).ConfigureAwait(true),
				cancellationToken);

			tasks.Add(everythingElseTask);

			return Task.WhenAll(tasks);
		}

		private async Task UpdateBatchedFeedAsync(List<Feed> largeGroup, BatchOptions batchOptions, CancellationToken cancellationToken)
		{
			int countTaken = 0;

			ParallelOptions batchedParallelOptions = new ParallelOptions
			{
				MaxDegreeOfParallelism = rdrOptionsMonitor.CurrentValue.UpdateConcurrency,
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

			feed.Status = FeedStatus.Updating;

			void configureRequest(HttpRequestMessage request)
			{
				request.Version = HttpVersion.Version20;

				string userAgentHeaderValue = rdrOptionsMonitor.CurrentValue.CustomUserAgent;

				if (!request.Headers.UserAgent.TryParseAdd(userAgentHeaderValue))
				{
					throw new HeaderException
					{
						UnaddableHeader = userAgentHeaderValue
					};
				}
			}

			StringResponse response;

			using (HttpClient client = httpClientFactory.CreateClient("RdrService"))
			{
				response = await Web.DownloadStringAsync(client, feed.Link, configureRequest, cancellationToken).ConfigureAwait(false);
			}

			if (response.Reason == Reason.ETagMatch)
			{
				feed.Status = FeedStatus.Ok;

				LogETagMatch(logger, feed.Name, feed.Link.AbsoluteUri);

				return;
			}

			if (response.Reason == Reason.Timeout)
			{
				feed.Status = FeedStatus.Timeout;

				LogTimeout(logger, feed.Name, feed.Link.AbsolutePath);
				
				return;
			}

			if (response.Reason != Reason.Success)
			{
				feed.Status = response.StatusCode switch
				{
					HttpStatusCode.Forbidden => FeedStatus.Forbidden,
					HttpStatusCode.Moved => FeedStatus.MovedCannotFollow,
					HttpStatusCode.NotFound => FeedStatus.DoesNotExist,
					_ => FeedStatus.OtherInternetError,
				};

				LogFeedUpdateFailed(logger, GetError(response), GetNameForLogMessage(feed));

				return;
			}

			if (!XmlHelpers.TryParse(response.Text, out XDocument? document))
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

		private static string GetError(StringResponse response)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append(response.Reason.ToString());

			if (response.StatusCode is HttpStatusCode statusCode)
			{
				sb.Append(" - ");
				sb.Append(FormatStatusCode(statusCode));
			}

			if (response.Exception is Exception ex)
			{
				Exception toLog = ex switch
				{
					HttpRequestException httpRequestException => httpRequestException.InnerException ?? httpRequestException,
					_ => ex
				};

				string? message = toLog switch
				{
					HttpRequestException httpRequestException => httpRequestException.HttpRequestError == HttpRequestError.Unknown
						? null
						: httpRequestException.HttpRequestError.ToString(),
					HttpIOException httpIOException => $"{nameof(HttpIOException)} ({httpIOException.HttpRequestError})",
					SocketException socketException => $"{nameof(SocketException)} ({socketException.SocketErrorCode})",
					OperationCanceledException operationCanceledException => null,
					_ => ex.GetType().FullName ?? ex.GetType().Name
				};

				if (!String.IsNullOrEmpty(message))
				{
					sb.Append(" - ");
					sb.Append(message);
				}
			}

			return sb.ToString();
		}

		private static string GetNameForLogMessage(Feed feed)
		{
			return String.Equals(feed.Name, feed.Link.AbsoluteUri, StringComparison.OrdinalIgnoreCase)
				? feed.Link.AbsoluteUri
				: feed.Name;
		}

		[System.Diagnostics.DebuggerStepThrough]
		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path)
			=> DownloadEnclosureAsyncInternal(enclosure, path, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress)
			=> DownloadEnclosureAsyncInternal(enclosure, path, progress, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, path, null, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, path, progress, cancellationToken);

		private async Task<FileResponse> DownloadEnclosureAsyncInternal(Enclosure enclosure, string path, IProgress<FileProgress>? progress, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(enclosure);
			ArgumentNullException.ThrowIfNull(path);

			using HttpClient client = httpClientFactory.CreateClient();

			Task<FileResponse> downloadEnclosureTask = progress is not null
				? Web.DownloadFileAsync(client, enclosure.Link, path, progress, cancellationToken)
				: Web.DownloadFileAsync(client, enclosure.Link, path, cancellationToken);

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

		[LoggerMessage(ETagMatchId, LogLevel.Trace, "etag match for '{FeedName}' ('{FeedLink}')")]
		internal static partial void LogETagMatch(ILogger<RdrService> logger, string feedName, string feedLink);
		
		[LoggerMessage(TimeoutId, LogLevel.Warning, "timeout for '{FeedName}' ('{FeedLink}')")]
		internal static partial void LogTimeout(ILogger<RdrService> logger, string feedName, string feedLink);

		[LoggerMessage(MarkAsReadId, LogLevel.Trace, "marked item as read: '{FeedName}'->'{ItemName}'")]
		internal static partial void LogMarkAsRead(ILogger<RdrService> logger, string feedName, string itemName);

		[LoggerMessage(MarkAllAsReadId, LogLevel.Debug, "marked ALL as read")]
		internal static partial void LogMarkAllAsRead(ILogger<RdrService> logger);

		[LoggerMessage(ClearFeedsId, LogLevel.Debug, "cleared ALL feeds")]
		internal static partial void LogClearFeeds(ILogger<RdrService> logger);
	}
}
