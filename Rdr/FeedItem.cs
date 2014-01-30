using System;
using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using System.Text;
using System.Windows;

namespace Rdr
{
    class FeedItem : RdrBase
    {
        #region Visible
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
                OnPropertyChanged("Unread");
            }
        }
        #endregion

        public FeedItem(SyndicationItem item, string feedTitle)
        {
            this.FeedItemTitle = DetermineItemTitle(item);
            this.TitleOfFeed = feedTitle;
            this.Published = item.PublishDate.LocalDateTime;
            this.Link = DetermineItemLink(item);
            this.Unread = true;
        }

        private string DetermineItemTitle(SyndicationItem item)
        {
            if (item.Title != null)
            {
                if (item.Title is TextSyndicationContent)
                {
                    if (item.Title.Text == string.Empty)
                    {
                        return "untitled";
                    }
                    else
                    {
                        return item.Title.Text;
                    }
                }
            }

            return "title unknown";
        }

        private string DetermineItemLink(SyndicationItem item)
        {
            if (item.Links != null)
            {
                if (item.Links is Collection<SyndicationLink>)
                {
                    if (item.Links.Count > 0)
                    {
                        return item.Links[0].Uri.AbsoluteUri;
                    }
                }
            }

            return string.Empty;
        }

        public override bool Equals(object obj)
        {
            if (this.GetHashCode() == obj.GetHashCode())
            {
                return true;
            }
            else
            {
                return false;
            }
        } // locked

        public override int GetHashCode()
        {
            return this.FeedItemTitle.GetHashCode();
        } // locked

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.FeedItemTitle);
            sb.AppendLine(this.TitleOfFeed);
            sb.AppendLine(this.Published.ToString());
            sb.AppendLine(this.Link);
            sb.AppendLine(this.Unread.ToString());

            return sb.ToString();
        } // locked
    }
}
