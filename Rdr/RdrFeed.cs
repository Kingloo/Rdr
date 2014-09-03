using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Rdr
{
    class RdrFeed : RdrBase
    {
        private string _name = "_name init";
        public string Name
        {
            get { return this._name; }
            set
            {
                this._name = value;
                OnNotifyPropertyChanged();
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
                OnNotifyPropertyChanged();
            }
        }

        public string Tooltip
        {
            get
            {
                return string.Format("{0} items, {1} unread", this.Items.Count, UnreadItemsCount());
            }
        }

        private ObservableCollection<RdrFeedItem> _items = new ObservableCollection<RdrFeedItem>();
        public ObservableCollection<RdrFeedItem> Items { get { return this._items; } }

        public RdrFeed(Uri xmlUrl)
        {
            this._xmlUrl = xmlUrl;
        }

        public void Load(XDocument x)
        {
            if (x.Root.Name.LocalName.Equals("feed"))
            {
                LoadWithSpecifications(x.Root, "entry");
            }
            else if (x.Root.Name.LocalName.Equals("rss"))
            {
                LoadWithSpecifications(x.Root.Element("channel"), "item");
            }
        }

        private void LoadWithSpecifications(XElement element, string nameOfRdrFeedItemTag)
        {
            foreach (XElement each in element.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    this.Name = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value;
                }
            }

            IEnumerable<RdrFeedItem> allItems = from each in element.Elements(nameOfRdrFeedItemTag)
                                                select new RdrFeedItem(each, this.Name);

            Console.WriteLine(string.Format("{0} items count: {1}", this.Name, allItems.Count<RdrFeedItem>().ToString()));

            this.Items.AddMissingItems<RdrFeedItem>(allItems);
        }

        public void MarkAllItemsAsRead()
        {
            foreach (RdrFeedItem each in this.Items)
            {
                each.MarkAsRead();
            }
        }

        private int UnreadItemsCount()
        {
            IEnumerable<RdrFeedItem> allUnreadItems = from each in this.Items
                                                      where each.Unread
                                                      select each;

            return allUnreadItems.Count<RdrFeedItem>();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(this.Name);
            sb.AppendLine(this.XmlUrl.AbsoluteUri);
            sb.AppendLine(this.Updating.ToString());
            sb.AppendLine(this.Tooltip);

            return sb.ToString();
        }
    }
}
