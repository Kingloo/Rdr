using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Rdr.Fidr
{
    abstract class Feed : IFeed, INotifyPropertyChanged
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

        private string _name = string.Empty;
        public string Name
        {
            get { return this._name; }
            set
            {
                this._name = value;
                OnNotifyPropertyChanged();
            }
        }

        private Uri _xmlUrl = null;
        public Uri XmlUrl
        {
            get { return this._xmlUrl; }
            set { this._xmlUrl = value; }
        }

        private string _generator = string.Empty;
        public string Generator
        {
            get { return this._generator; }
            set { this._generator = value; }
        }

        private DateTime _lastBuildDate = DateTime.MinValue;
        public DateTime LastBuildDate
        {
            get { return this._lastBuildDate; }
            set { this._lastBuildDate = value; }
        }

        private bool _updating = false;
        public bool Updating
        {
            get { return this._updating; }
            set
            {
                this._updating = value;
                OnNotifyPropertyChanged();
            }
        }

        public string Tooltip
        {
            get
            {
                return string.Format("{0} items, {1} unread", this.FeedItems.Count, UnreadItemsCount());
            }
        }

        private IFeedImage _image = null;
        public IFeedImage Image
        {
            get { return this._image; }
            set { this._image = value; }
        }

        protected ObservableCollection<IFeedItem> _feedItems = new ObservableCollection<IFeedItem>();
        public ObservableCollection<IFeedItem> FeedItems { get { return this._feedItems; } }

        public abstract void Load(string s);

        public void MarkAllItemsAsRead()
        {
            foreach (IFeedItem each in this.FeedItems)
            {
                each.MarkAsRead();
            }
        }

        private int UnreadItemsCount()
        {
            IEnumerable<IFeedItem> allUnreadItems = from each in this.FeedItems
                                                    where each.Unread
                                                    select each;

            return allUnreadItems.Count<IFeedItem>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("Name: {0}", this._name));
            sb.AppendLine(this._xmlUrl == null ? "Link: none" : string.Format("Link: {0}", this._xmlUrl.AbsoluteUri));
            sb.AppendLine(string.Format("Generator: {0}", this._generator));
            sb.AppendLine(string.Format("Last build date: {0}", this._lastBuildDate.ToString()));
            sb.AppendLine(this._image == null ? "Image: none" : string.Format("Image: {0}", this._image.ToString()));
            sb.AppendLine(string.Format("No. of items: {0}", this._feedItems.Count.ToString()));

            return sb.ToString();
        }
    }
}