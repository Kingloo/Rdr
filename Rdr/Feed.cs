using System;
using System.Collections.Generic;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading.Tasks;

namespace Rdr
{
    class Feed : RdrBase
    {
        #region Visible
        private string _feedTitle = string.Empty;
        public string FeedTitle
        {
            get { return this._feedTitle; }
            set
            {
                this._feedTitle = value;
                OnPropertyChanged("FeedTitle");
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
            this.FeedTitle = xmlurl.AbsoluteUri;
            this.XmlUrl = xmlurl;
            this.FeedItems = new List<FeedItem>();
            this.IsFirstLoad = true;
        } // locked

        public Feed(string message)
        {
            this.FeedTitle = message;

            Uri uri = null;
            if (Uri.TryCreate(message, UriKind.Absolute, out uri))
            {
                this.XmlUrl = uri;
            }
            else
            {
                throw new ArgumentException(string.Format("Feed URL is not valid: {0}", message));
            }
        } // locked

        public List<FeedItem> FirstLoad(SyndicationFeed xmlFeed, int max)
        {
            this.FeedTitle = DetermineFeedTitle(xmlFeed.Title);

            foreach (SyndicationItem item in xmlFeed.Items)
            {
                FeedItem feedItem = new FeedItem(item, FeedTitle);

                this.FeedItems.Add(feedItem);
            }

            List<FeedItem> toReturn = new List<FeedItem>();

            // evalute (max > this.FeedItems.Count) -> if true use this.FeedItems.Count, if false use max
            int range = (max > this.FeedItems.Count) ? this.FeedItems.Count : max;

            for (int i = 0; i < range; i++)
            {
                toReturn.Add(this.FeedItems[i]);
            }

            this.IsFirstLoad = false;
            return toReturn;
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
            foreach (FeedItem each in this.FeedItems)
            {
                if (test.Equals(each))
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
        } // locked

        public Task MarkAllItemsAsReadAsync()
        {
            return Task.Factory.StartNew(new Action(
                delegate()
                {
                    foreach (FeedItem feedItem in this.FeedItems)
                    {
                        feedItem.Unread = false;
                    }
                }));
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
        } // locked

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.FeedTitle);
            sb.AppendLine(this.XmlUrl.AbsoluteUri);
            sb.AppendLine(this.IsFirstLoad.ToString());
            sb.AppendLine(string.Format("Feed Items: {0}", this.FeedItems.Count));

            return sb.ToString();
        } // locked
    }
}
