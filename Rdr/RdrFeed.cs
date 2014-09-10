using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Rdr
{
    class RdrFeed : RdrBase
    {
        private enum FeedType { None, Atom, RSS };

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
                this.Name = GetTitle(x.Root);

                LoadFromXElement(x.Root.Elements(XName.Get("entry", "http://www.w3.org/2005/Atom")));
            }
            else if (x.Root.Name.LocalName.Equals("rss"))
            {
                this.Name = GetTitle(x.Root.Element("channel"));

                LoadFromXElement(x.Root.Element("channel").Elements("item"));
            }
        }

        private string GetTitle(XElement e)
        {
            string toReturn = this.XmlUrl.AbsoluteUri;

            foreach (XElement each in e.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    if (String.IsNullOrWhiteSpace(each.Value) == false)
                    {
                        toReturn = each.Value;

                        break;
                    }
                }
            }

            return toReturn;
        }

        private void LoadFromXElement(IEnumerable<XElement> e)
        {
            IEnumerable<RdrFeedItem> allItems = from each in e
                                                select new RdrFeedItem(each, this.Name);

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
