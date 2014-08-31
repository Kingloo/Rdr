using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rdr.Fidr
{
    abstract class FeedItem : IEquatable<FeedItem>, IComparable<FeedItem>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnNotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChangedEventHandler pceh = this.PropertyChanged;
            if (pceh != null)
            {
                pceh(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected string _name = string.Empty;
        public string Name { get { return this._name; } }

        protected string _titleOfFeed = string.Empty;
        public string TitleOfFeed { get { return this._titleOfFeed; } }

        private bool _unread = true;
        public bool Unread
        {
            get { return this._unread; }
            set
            {
                this._unread = value;
                OnNotifyPropertyChanged();
            }
        }

        protected string _description = string.Empty;
        public string Description { get { return this._description; } }

        protected string _author = string.Empty;
        public string Author { get { return this._author; } }

        protected DateTime _pubDate = DateTime.MinValue;
        public DateTime PubDate { get { return this._pubDate; } }

        protected Uri _link = null;
        public Uri Link { get { return this._link; } }

        protected FeedEnclosure _enclosure = null;
        public FeedEnclosure Enclosure { get { return this._enclosure; } }

        protected bool _hasEnclosure = false;
        public bool HasEnclosure { get { return this._hasEnclosure; } }

        public void MarkAsRead()
        {
            this.Unread = false;
        }

        public bool Equals(FeedItem other)
        {
            if (other.Name.Equals(this.Name) == false)
            {
                return false;
            }

            return true;
        }

        public int CompareTo(FeedItem other)
        {
            if (this.PubDate > other.PubDate)
            {
                //return 1;
                return -1;
            }
            else if (this.PubDate < other.PubDate)
            {
                //return -1;
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("Title: {0}", this.Name));
            sb.AppendLine(string.Format("Description: {0}", this.Description.Substring(0, ((this.Description.Length > 30) ? 30 : this.Description.Length))));
            sb.AppendLine(string.Format("Author: {0}", this.Author));
            sb.AppendLine(string.Format("PubDate: {0}", this.PubDate.ToString()));
            sb.AppendLine(this.Link == null ? "Link: no link" : string.Format("Link: {0}", this.Link.AbsoluteUri));
            sb.AppendLine(this.Enclosure == null ? "Enclosure: no enclosure" : string.Format("Enclosure:{0}{1}", Environment.NewLine, this.Enclosure.ToString()));

            return sb.ToString();
        }
    }
}
