using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdrLib.Exceptions;
using RdrLib.Helpers;
using RdrLib.Model;
using static RdrLib.EventIds.RdrService;

namespace RdrLib
{
	public class RdrService : IRdrService
	{
		private readonly ObservableCollection<Feed> feeds = new ObservableCollection<Feed>();
		public IReadOnlyCollection<Feed> Feeds { get => feeds; }

		private readonly IOptionsMonitor<RdrOptions> rdrOptionsMonitor;
		private readonly ILogger<RdrService> logger;

		public RdrService(IOptionsMonitor<RdrOptions> rdrOptionsMonitor, ILogger<RdrService> logger)
		{
			ArgumentNullException.ThrowIfNull(rdrOptionsMonitor);
			ArgumentNullException.ThrowIfNull(logger);

			this.rdrOptionsMonitor = rdrOptionsMonitor;
			this.logger = logger;
		}

		public bool Add(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			if (!feeds.Contains(feed))
			{
				feeds.Add(feed);

				logger.LogDebug(FeedAdded, "feed added ({FeedLink})", feed.Link.AbsoluteUri);

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
				logger.LogDebug(FeedRemoved, "feed removed ({FeedLink})", feed.Link.AbsoluteUri);
			}
			else
			{
				logger.LogWarning(FeedRemovedFailed, "failed to remove feed ({FeedLink})", feed.Link.AbsoluteUri);
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

			logger.LogTrace("marked item as read: '{FeedName}'->'{ItemName}'", item.FeedName, item.Name);
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

			logger.LogTrace("marked ALL as read");
		}

		public void ClearFeeds()
		{
			feeds.Clear();

			logger.LogDebug("cleared ALL feeds");
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
			=> UpdateAsync(feeds, CancellationToken.None);

		public async Task UpdateAsync(IEnumerable<Feed> feeds, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(feeds);

			ParallelOptions parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = rdrOptionsMonitor.CurrentValue.UpdateConcurrency
			};

			await Parallel.ForEachAsync(feeds, parallelOptions, UpdateFeedAsync).ConfigureAwait(true);
		}

		private async ValueTask UpdateFeedAsync(Feed feed, CancellationToken cancellationToken)
		{
			logger.LogDebug(FeedUpdateStarted, "updating {FeedName} ({FeedLink})", feed.Name, feed.Link.AbsoluteUri);

			feed.Status = FeedStatus.Updating;

			void configureRequest(HttpRequestMessage request)
			{
				string userAgentHeaderValue = rdrOptionsMonitor.CurrentValue.CustomUserAgent;

				if (!request.Headers.UserAgent.TryParseAdd(userAgentHeaderValue))
				{
					throw new HeaderException
					{
						UnaddableHeader = userAgentHeaderValue
					};
				}
			}

			StringResponse response = await Web.DownloadStringAsync(feed.Link, configureRequest, cancellationToken).ConfigureAwait(false);

			if (response.Reason != Reason.Success)
			{
				feed.Status = response.StatusCode switch
				{
					HttpStatusCode.Forbidden => FeedStatus.Forbidden,
					HttpStatusCode.Moved => FeedStatus.MovedCannotFollow,
					HttpStatusCode.NotFound => FeedStatus.DoesNotExist,
					_ => FeedStatus.OtherInternetError,
				};

				logger.LogWarning(
					FeedUpdateFailed,
					"update failed: {StatusCode} for '{FeedName}' ({FeedLink})",
					response.StatusCode,
					feed.Name,
					feed.Link.AbsoluteUri);

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

			logger.LogDebug(FeedUpdateSucceeded, "updated '{FeedName}' ({FeedLink})", feed.Name, feed.Link.AbsoluteUri);
		}

		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path)
			=> DownloadEnclosureAsyncInternal(enclosure, path, null, CancellationToken.None);

		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress)
			=> DownloadEnclosureAsyncInternal(enclosure, path, progress, CancellationToken.None);

		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, path, null, cancellationToken);

		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, path, progress, cancellationToken);

		private static Task<FileResponse> DownloadEnclosureAsyncInternal(Enclosure enclosure, string path, IProgress<FileProgress>? progress, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(enclosure);
			ArgumentNullException.ThrowIfNull(path);

			return (progress is not null) switch
			{
				true => Web.DownloadFileAsync(enclosure.Link, path, progress, cancellationToken),
				false => Web.DownloadFileAsync(enclosure.Link, path, cancellationToken)
			};
		}
	}
}
