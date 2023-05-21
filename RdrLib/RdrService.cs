using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using RdrLib.Exceptions;
using RdrLib.Helpers;
using RdrLib.Model;

namespace RdrLib
{
	public class RdrService : IRdrService
	{
		private readonly ObservableCollection<Feed> _feeds = new ObservableCollection<Feed>();
		public IReadOnlyCollection<Feed> Feeds { get => _feeds; }

		public RdrService() { }

		public bool Add(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			if (!_feeds.Contains(feed))
			{
				_feeds.Add(feed);

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

			if (_feeds.Contains(feed))
			{
				return _feeds.Remove(feed);
			}
			else
			{
				return false;
			}
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
			foreach (Feed feed in _feeds)
			{
				foreach (Item item in feed.Items)
				{
					MarkAsRead(item);
				}
			}
		}

		public void Clear() => _feeds.Clear();

		public ValueTask UpdateAsync(Feed feed)
			=> UpdateAsync(feed, CancellationToken.None);

		public ValueTask UpdateAsync(Feed feed, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(feed);

			return UpdateFeedAsync(feed, cancellationToken);
		}

		public ValueTask UpdateAsync(IEnumerable<Feed> feeds)
			=> UpdateAsync(feeds, CancellationToken.None);

		public async ValueTask UpdateAsync(IEnumerable<Feed> feeds, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(feeds);

			await Parallel.ForEachAsync(feeds, cancellationToken, UpdateFeedAsync).ConfigureAwait(true);
		}

		private async ValueTask UpdateFeedAsync(Feed feed, CancellationToken cancellationToken)
		{
			feed.Status = FeedStatus.Updating;

			static void configRequest(HttpRequestMessage request)
			{
				string userAgentHeaderValue = UserAgents.Get(UserAgents.Firefox_102_Windows);

				if (!request.Headers.UserAgent.TryParseAdd(userAgentHeaderValue))
				{
					throw new HeaderException
					{
						UnaddableHeader = userAgentHeaderValue
					};
				}
			}

			StringResponse response = await Web.DownloadStringAsync(feed.Link, configRequest, cancellationToken).ConfigureAwait(false);

			if (response.Reason != Reason.Success)
			{
				feed.Status = response.Status switch
				{
					HttpStatusCode.Forbidden => FeedStatus.Forbidden,
					HttpStatusCode.Moved => FeedStatus.MovedCannotFollow,
					HttpStatusCode.NotFound => FeedStatus.DoesNotExist,
					_ => FeedStatus.OtherInternetError,
				};

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
		}

		public ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path)
			=> DownloadEnclosureAsyncInternal(enclosure, path, null, CancellationToken.None);

		public ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress)
			=> DownloadEnclosureAsyncInternal(enclosure, path, progress, CancellationToken.None);

		public ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, path, null, cancellationToken);

		public ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, path, progress, cancellationToken);

		private static ValueTask<FileResponse> DownloadEnclosureAsyncInternal(Enclosure enclosure, string path, IProgress<FileProgress>? progress, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(enclosure);

			if (String.IsNullOrWhiteSpace(path))
			{
				throw new ArgumentNullException(nameof(path));
			}

			return (progress is not null) switch
			{
				true => Web.DownloadFileAsync(enclosure.Link, path, progress, cancellationToken),
				false => Web.DownloadFileAsync(enclosure.Link, path, cancellationToken)
			};
		}
	}
}
