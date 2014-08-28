using System;
using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using System.Text;
using System.Windows;

namespace Rdr
{
    class FeedItem : RdrBase, IEquatable<FeedItem>
    {
        #region Properties
        public string FeedItemTitle { get; private set; }
        public string TitleOfFeed { get; private set; }
        public DateTime Published { get; private set; }
        public string Link { get; private set; }
        private bool _unread = true;
        public bool Unread
        {
            get { return this._unread; }
            set
            {
                this._unread = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public FeedItem(SyndicationItem item, string feedTitle)
        {
            this.FeedItemTitle = DetermineItemTitle(item);
            this.TitleOfFeed = feedTitle;
            this.Published = item.PublishDate.LocalDateTime;
            this.Link = DetermineItemLink(item);
        }

        private string DetermineItemTitle(SyndicationItem item)
        {
            if (item.Title != null)
            {
                return String.IsNullOrEmpty(item.Title.Text) ? "untitled" : item.Title.Text;
            }

            return "untitled";
        }

        private string DetermineItemLink(SyndicationItem item)
        {
            if (item.Links != null)
            {
                if (item.Links.Count > 0)
                {
                    return item.Links[0].Uri.AbsoluteUri;
                }
            }

            return string.Empty;
        }

        public void MarkAsRead()
        {
            this.Unread = false;
        }

        public bool Equals(FeedItem other)
        {
            if (other.FeedItemTitle.Equals(this.FeedItemTitle) == false)
            {
                return false;
            }

            if (other.TitleOfFeed.Equals(this.TitleOfFeed) == false)
            {
                return false;
            }

            if (other.Published.Equals(this.Published) == false)
            {
                return false;
            }

            if (other.Link.Equals(this.Link) == false)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.FeedItemTitle);
            sb.AppendLine(this.TitleOfFeed);
            sb.AppendLine(this.Published.ToString());
            sb.AppendLine(this.Link);
            sb.AppendLine(this.Unread.ToString());

            return sb.ToString();
        }
    }
}
