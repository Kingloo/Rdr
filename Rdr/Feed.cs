using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text;

namespace Rdr
{
    class Feed : RdrBase
    {
        #region Visible
        private string _title = string.Empty;
        public string Title
        {
            get { return this._title; }
            set
            {
                this._title = value;
                OnPropertyChanged("Title");
            }
        }
        public Uri XmlUrl { get; private set; }
        public List<FeedItem> FeedItems { get; private set; }
        public bool IsFirstLoad { get; private set; }
        public string ItemsCount
        {
            get
            {
                return string.Format("{0} items, {1} unread", this.FeedItems.Count, UnreadItemsCount());
            }
        }
        #endregion

        public Feed(Uri xmlurl)
        {
            this.Title = xmlurl.AbsoluteUri;
            this.XmlUrl = xmlurl;
            this.FeedItems = new List<FeedItem>();
            this.IsFirstLoad = true;
        }

        public Feed(string message)
        {
            this.Title = message;
            this.XmlUrl = new Uri(message);
        }

        public List<FeedItem> FirstLoad(SyndicationFeed xmlFeed, int max)
        {
            this.Title = DetermineFeedTitle(xmlFeed.Title);

            // add every item from the feed to FeedItems
            foreach (SyndicationItem item in xmlFeed.Items)
            {
                FeedItem feedItem = new FeedItem(item, xmlFeed.Title.Text);

                this.FeedItems.Add(feedItem);
            }

            this.FeedItems.Sort(sortFeedItemsByDate);

            List<FeedItem> toReturn = new List<FeedItem>();

            int range = (max > this.FeedItems.Count) ? this.FeedItems.Count : max;

            // build a list of the number of items to return to the UI
            for (int i = 0; i < range; i++)
            {
                toReturn.Add(this.FeedItems[i]);
            }

            this.IsFirstLoad = false;
            return toReturn;
        }

        private int sortFeedItemsByDate(FeedItem x, FeedItem y)
        {
            if (x.Published > y.Published)
            {
                return -1;
            }
            else if (y.Published > x.Published)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public List<FeedItem> Load(SyndicationFeed xmlFeed)
        {
            List<FeedItem> allFeedItems = new List<FeedItem>();

            foreach (SyndicationItem item in xmlFeed.Items)
            {
                FeedItem feedItem = new FeedItem(item, xmlFeed.Title.Text);
                allFeedItems.Add(feedItem);
            }

            List<FeedItem> itemsToAdd = new List<FeedItem>();
            foreach (FeedItem test in allFeedItems)
            {
                if (FeedItemsContains(test) == false)
                {
                    itemsToAdd.Add(test);
                    this.FeedItems.Add(test);
                }
            }

            return new List<FeedItem>(itemsToAdd);
        }

        private bool FeedItemsContains(FeedItem test)
        {
            foreach (FeedItem feedItem in this.FeedItems)
            {
                if (test.Title.Equals(feedItem.Title, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private string DetermineFeedTitle(TextSyndicationContent tsc)
        {
            if (tsc.Text != string.Empty)
            {
                return tsc.Text;
            }
            else
            {
                return this.XmlUrl.AbsoluteUri;
            }
        }

        public void MarkAllItemsAsRead()
        {
            lock (this.FeedItems)
            {
                foreach (FeedItem feedItem in this.FeedItems)
                {
                    feedItem.Unread = false;
                }
            }
        }

        private int UnreadItemsCount()
        {
            int count = 0;

            foreach (FeedItem feedItem in this.FeedItems)
            {
                if (feedItem.Unread)
                {
                    count++;
                }
            }

            return count;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.Title);
            sb.AppendLine(this.XmlUrl.AbsoluteUri);
            sb.AppendLine(this.IsFirstLoad.ToString());
            sb.AppendLine(string.Format("Feed Items: {0}", this.FeedItems.Count));

            return sb.ToString();
        }
    }
}
