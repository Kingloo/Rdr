using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Rdr.Extensions;

namespace Rdr
{
    class RdrFeed : RdrBase, IEquatable<RdrFeed>, IComparable<RdrFeed>, IAlternativeSort
    {
        private enum FeedType { None, Atom, RSS };

        #region Properties
        private string _name = ".name init";
        public string Name
        {
            get
            {
                return this._name;
            }
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
            get
            {
                return this._updating;
            }
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
                StringBuilder sb = new StringBuilder();

                sb.Append(string.Format("{0} items, {1} unread", this.Items.Count, UnreadItemsCount()));

                if (this.XmlUrl != null)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(this.XmlUrl.AbsoluteUri);
                }

                return sb.ToString();
            }
        }

        private int _sortId = 0;
        public int SortId
        {
            get
            {
                return _sortId;
            }
            set
            {
                _sortId = value;

                OnNotifyPropertyChanged();
            }
        }

        private ObservableCollection<RdrFeedItem> _items = new ObservableCollection<RdrFeedItem>();
        public ObservableCollection<RdrFeedItem> Items { get { return this._items; } }
        #endregion

        public RdrFeed(string name)
        {
            this.Name = name;
        }

        public RdrFeed(Uri xmlUrl)
        {
            this._xmlUrl = xmlUrl;
            this._name = xmlUrl.AbsoluteUri;
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

            toReturn = toReturn.RemoveUnicodeCategories(new List<UnicodeCategory>
            {
                UnicodeCategory.OtherSymbol
            });

            toReturn = toReturn.Trim();

            return toReturn;
        }

        private void LoadFromXElement(IEnumerable<XElement> e)
        {
            IEnumerable<RdrFeedItem> allItems = from each in e
                                                select new RdrFeedItem(each, Name);

            IEnumerable<RdrFeedItem> fromLastSevenDays = from each in allItems
                                                         where each.PubDate > (DateTime.Now - TimeSpan.FromDays(10))
                                                         select each;

            Items.AddMissing<RdrFeedItem>(fromLastSevenDays);
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

        public int CompareTo(RdrFeed other)
        {
            return this.Name.CompareTo(other.Name);
        }

        public bool Equals(RdrFeed other)
        {
            if ((this.XmlUrl != null) && (other.XmlUrl != null))
            {
                return this.XmlUrl.AbsoluteUri.Equals(other.XmlUrl.AbsoluteUri);
            }
            else
            {
                return this.Name.Equals(other.Name);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(this.Name);
            if (this.XmlUrl != null) sb.AppendLine(this.XmlUrl.AbsoluteUri);
            sb.AppendLine(this.Updating.ToString());
            sb.AppendLine(this.Tooltip);
            sb.AppendLine(this.SortId.ToString());

            return sb.ToString();
        }
    }
}
