using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using RdrLib.Common;
using RdrLib.Helpers;
using RdrLib.Model;

namespace RdrLib
{
	public class RdrService
	{
		private readonly ObservableCollection<Feed> _feeds = new ObservableCollection<Feed>();
		public IReadOnlyCollection<Feed> Feeds { get => _feeds; }

		public RdrService() { }

		[System.Diagnostics.DebuggerStepThrough]
		public Task UpdateAsync(Feed feed)
			=> UpdateFeedAsync(feed);

		public Task UpdateAsync(IEnumerable<Feed> feedsToUpdate)
		{
			Collection<Task> tasks = new Collection<Task>();

			foreach (Feed feed in feedsToUpdate)
			{
				Task task = Task.Run(() => UpdateAsync(feed));

				tasks.Add(task);
			}

			return Task.WhenAll(tasks);
		}

		[System.Diagnostics.DebuggerStepThrough]
		public Task UpdateAllAsync()
			=> UpdateAsync(_feeds);

		private async Task UpdateFeedAsync(Feed feed)
		{
			feed.Status = FeedStatus.Updating;

			static void configRequest(HttpRequestMessage request)
			{
				request.Headers.UserAgent.ParseAdd(UserAgents.Firefox_94_Windows);
			}

			StringResponse response = await Web.DownloadStringAsync(feed.Link, configRequest).ConfigureAwait(false);

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

		[System.Diagnostics.DebuggerStepThrough]
		public Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path)
			=> DownloadEnclosureAsync(enclosure, path, null);

		public async Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress>? progress)
		{
			if (progress is null)
			{
				return await Web.DownloadFileAsync(enclosure.Link, path).ConfigureAwait(false);
			}
			else
			{
				return await Web.DownloadFileAsync(enclosure.Link, path, progress).ConfigureAwait(false);
			}
		}

		public bool Add(Feed feed)
		{
			if (!_feeds.Contains(feed))
			{
				_feeds.Add(feed);

				return true;
			}

			return false;
		}

		public int Add(IReadOnlyCollection<Feed> feeds)
		{
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
			if (_feeds.Contains(feed))
			{
				return _feeds.Remove(feed);
			}
			else
			{
				return false;
			}
		}

		public int Remove(IReadOnlyCollection<Feed> feeds)
		{
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

		public void Clear() => _feeds.Clear();

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

		public void MarkAsRead(Item item) => item.Unread = false;
	}
}
