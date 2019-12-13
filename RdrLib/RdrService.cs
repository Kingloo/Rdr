using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using RdrLib.Helpers;
using RdrLib.Model;

namespace RdrLib
{
    public class RdrService
    {
        private readonly ObservableCollection<Feed> _feeds = new ObservableCollection<Feed>();
        public IReadOnlyCollection<Feed> Feeds => _feeds;
        
        public RdrService() { }

        public Task UpdateAsync(Feed feed) => UpdateFeedAsync(feed);

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

        public Task UpdateAllAsync() => UpdateAsync(_feeds);

        private async Task UpdateFeedAsync(Feed feed)
        {
            feed.Status = FeedStatus.Updating;

            (HttpStatusCode code, string text) = await Download.StringAsync(feed.Link).ConfigureAwait(false);

            if (code != HttpStatusCode.OK)
            {
                feed.Status = code switch
                {
                    HttpStatusCode.Forbidden => FeedStatus.Forbidden,
                    HttpStatusCode.Moved => FeedStatus.MovedCannotFollow,
                    HttpStatusCode.NotFound => FeedStatus.DoesNotExist,
                    _ => FeedStatus.OtherInternetError,
                };

                return;
            }

            if (!XmlHelpers.TryParse(text, out XDocument? document))
            {
                feed.Status = FeedStatus.Broken;
                return;
            }

            feed.Name = FeedHelpers.GetName(document);

            IEnumerable<Item> items = FeedHelpers.GetItems(document, feed.Name);

            feed.AddMany(items);

            feed.Status = FeedStatus.None;
        }

        public Task<DownloadResult> DownloadEnclosureAsync(Enclosure enclosure, string path)
            => DownloadEnclosureAsync(enclosure, path, null);

        public async Task<DownloadResult> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<DownloadProgress>? progress)
        {
            if (enclosure.Link is null)
            {
                return DownloadResult.NoLink;
            }

            if (progress is null)
            {
                Download download = new Download(enclosure.Link, path);
                
                return await download.ToFileAsync().ConfigureAwait(false);
            }
            else
            {
                Download download = new Download(enclosure.Link, path);

                return await download.ToFileAsync(progress, CancellationToken.None).ConfigureAwait(false);
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
                _feeds.Remove(feed);

                return true;
            }

            return false;
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
