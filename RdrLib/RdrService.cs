using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly bool preserveSynchronizationContext = true;

        private readonly ObservableCollection<Feed> _feeds = new ObservableCollection<Feed>();
        public IReadOnlyCollection<Feed> Feeds => _feeds;
        
        public RdrService()
            : this(true)
        { }

        public RdrService(bool preserveSynchronizationContext)
        {
            this.preserveSynchronizationContext = preserveSynchronizationContext;
        }

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

            (HttpStatusCode code, string text) = await Download.StringAsync(feed.Link).ConfigureAwait(preserveSynchronizationContext);

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
                feed.Status = FeedStatus.ParseFailed;
                return;
            }

            feed.Name = FeedHelpers.GetName(document);

            IReadOnlyCollection<Item> items = FeedHelpers.GetItems(document, feed.Name);

            feed.AddMany(items);




            // DEBUG ONLY !

            //feed.AddMany(items.Take(5));

            //if (feed.Name.Contains("varney", StringComparison.OrdinalIgnoreCase))
            //{
            //    Item fake = new Item(feed.Name)
            //    {
            //        Name = DateTimeOffset.Now.Ticks.ToString(),
            //        Published = DateTimeOffset.Now,
            //        Unread = true
            //    };

            //    feed.Add(fake);
            //}



            //




            feed.Status = FeedStatus.Ok;
        }

        public Task<DownloadResult> DownloadEnclosureAsync(Enclosure enclosure, string path)
            => DownloadEnclosureAsync(enclosure, path, null);

        public Task<DownloadResult> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<DownloadProgress>? progress)
        {
            if (progress is null)
            {
                Download download = new Download(enclosure.Link, path);
                
                return download.ToFileAsync();
            }
            else
            {
                Download download = new Download(enclosure.Link, path);

                return download.ToFileAsync(progress, CancellationToken.None);
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

        public void CleanUp() => Download.CleanUp();
    }
}
