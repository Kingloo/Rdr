using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Rdr.Extensions;

namespace Rdr.Model
{
    public class RdrFeed : ViewModelBase, IEquatable<RdrFeed>, IComparable<RdrFeed>, IAlternativeSort
    {
        private enum FeedType { None, Atom, RSS };

        #region Fields
        private readonly TimeSpan daysWorthOfItemsToKeep = TimeSpan.FromDays(9);
        #endregion

        #region Properties
        private string _name = ".name init";
        public string Name
        {
            get => _name;
            set
            {
                //if (_name != value)
                //{
                //    _name = value;

                //    RaisePropertyChanged(nameof(Name));
                //}

                _name = value;

                RaisePropertyChanged(nameof(Name));
            }
        }

        private readonly Uri _xmlUrl = null;
        public Uri XmlUrl => _xmlUrl;

        private bool _updating = false;
        public bool Updating
        {
            get => _updating;
            set
            {
                if (_updating != value)
                {
                    _updating = value;

                    RaisePropertyChanged(nameof(Updating));
                }
            }
        }

        public string Tooltip
        {
            get
            {
                StringBuilder sb = new StringBuilder();

                sb.Append(string.Format(CultureInfo.CurrentCulture, "{0} items, {1} unread", Items.Count, UnreadItemsCount()));

                if (XmlUrl != null)
                {
                    sb.Append(Environment.NewLine);
                    sb.Append(XmlUrl.AbsoluteUri);
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
                //if (_sortId != value)
                //{
                //    _sortId = value;

                //    RaisePropertyChanged(nameof(SortId));
                //}

                _sortId = value;

                RaisePropertyChanged(nameof(SortId));
            }
        }

        private ObservableCollection<RdrFeedItem> _items
            = new ObservableCollection<RdrFeedItem>();
        public IReadOnlyCollection<RdrFeedItem> Items => _items;
        #endregion

        public RdrFeed(string name)
        {
            Name = name;
        }

        public RdrFeed(Uri xmlUrl)
        {
            _xmlUrl = xmlUrl ?? throw new ArgumentNullException(nameof(xmlUrl));
            _name = xmlUrl.AbsoluteUri;
        }
        
        public void Load(XDocument doc)
        {
            if (doc == null) { throw new ArgumentNullException(nameof(doc)); }

            if (doc.Root.Name.LocalName.Equals("feed"))
            {
                Name = GetTitle(doc.Root);

                LoadFromXElement(doc.Root.Elements(XName.Get("entry", "http://www.w3.org/2005/Atom")));
            }
            else if (doc.Root.Name.LocalName.Equals("rss"))
            {
                Name = GetTitle(doc.Root.Element("channel"));

                LoadFromXElement(doc.Root.Element("channel").Elements("item"));
            }
        }

        private string GetTitle(XElement e)
        {
            var titles = e.Elements()
                .Where(x => x.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase)
                && !String.IsNullOrWhiteSpace(x.Value));

            string title = XmlUrl.AbsoluteUri;

            if (titles.Any())
            {
                title = titles
                    .First()
                    .Value
                    .RemoveUnicodeCategories(new List<UnicodeCategory>
                    {
                        UnicodeCategory.OtherSymbol
                    })
                    .Trim();
            }

            return title;
        }

        private void LoadFromXElement(IEnumerable<XElement> e)
        {
            // .Where(x => x.PubDate > (DateTime.Now - daysWorthOfItemsToKeep))

            var itemsFromLastFewDays = e
                .Select(x => new RdrFeedItem(x, Name))
                .Where(x => x.PubDate > (DateTime.Now - daysWorthOfItemsToKeep))
                .ToList();
            
            _items.AddMissing(itemsFromLastFewDays);
        }

        public void AddMissingItems(IEnumerable<RdrFeedItem> items) => _items.AddMissing(items);

        public void ClearItems() => _items.Clear();

        public void RemoveItem(RdrFeedItem item)
        {
            if (_items.Contains(item))
            {
                _items.Remove(item);
            }
        }

        public void MarkAllItemsAsRead()
        {
            foreach (RdrFeedItem each in Items)
            {
                each.MarkAsRead();
            }
        }

        private int UnreadItemsCount() => _items.Where(x => x.Unread).Count();

        public int CompareTo(RdrFeed other)
        {
            if (other == null) { throw new ArgumentNullException(nameof(other)); }

            return String.Compare(Name, other.Name, StringComparison.CurrentCulture);
        }

        public bool Equals(RdrFeed other)
        {
            if (other == null) { return false; }

            if ((XmlUrl != null) && (other.XmlUrl != null))
            {
                return XmlUrl.AbsoluteUri.Equals(other.XmlUrl.AbsoluteUri);
            }
            else
            {
                return Name.Equals(other.Name);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetType().Name);
            sb.AppendLine(Name);

            if (XmlUrl != null)
            {
                sb.AppendLine(XmlUrl.AbsoluteUri);
            }

            sb.AppendLine(Updating.ToString());
            sb.AppendLine(Tooltip);
            sb.AppendLine(SortId.ToString(CultureInfo.CurrentCulture));

            return sb.ToString();
        }
    }
}
