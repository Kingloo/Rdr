using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;

namespace Rdr
{
    class Feed : RdrBase, IEquatable<Feed>
    {
        #region Properties
        private string _feedTitle = string.Empty;
        public string FeedTitle
        {
            get { return this._feedTitle; }
            set
            {
                this._feedTitle = value;
                OnPropertyChanged();
            }
        }

        private readonly Uri _xmlUrl = null;
        public Uri XmlUrl { get { return this._xmlUrl; } }

        private bool _updating = false;
        public bool Updating
        {
            get { return this._updating; }
            set
            {
                this._updating = value;
                OnPropertyChanged();
            }
        }

        private List<FeedItem> _feedItems = new List<FeedItem>();
        public List<FeedItem> FeedItems { get { return this._feedItems; } }

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
            this._xmlUrl = xmlurl;
            this._feedTitle = xmlurl.AbsoluteUri;
        }

        public void Load(SyndicationFeed xmlFeed)
        {
            this.FeedTitle = DetermineFeedTitle(xmlFeed.Title);

            IEnumerable<FeedItem> allItemsInFeed = from each in xmlFeed.Items
                                                   select new FeedItem(each, xmlFeed.Title.Text);

            this.FeedItems.AddMissingItems<FeedItem>(allItemsInFeed);
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
            foreach (FeedItem each in this.FeedItems)
            {
                each.MarkAsRead();
            }
        }

        private int UnreadItemsCount()
        {
            IEnumerable<FeedItem> unreadItems = from each in this.FeedItems
                                                where each.Unread
                                                select each;

            return unreadItems.Count<FeedItem>();
        }

        public bool Equals(Feed other)
        {
            if (other.FeedTitle.Equals(this.FeedTitle) == false)
            {
                return false;
            }

            if (other.XmlUrl.AbsoluteUri.Equals(this.XmlUrl.AbsoluteUri) == false)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(this.FeedTitle);
            sb.AppendLine(this.XmlUrl.AbsoluteUri);
            sb.AppendLine(string.Format("Feed Items: {0}", this.FeedItems.Count));

            return sb.ToString();
        }
    }
}
