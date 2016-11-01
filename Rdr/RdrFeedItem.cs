using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Rdr.Extensions;

namespace Rdr
{
    public class RdrFeedItem : ViewModelBase, IEquatable<RdrFeedItem>, IComparable<RdrFeedItem>
    {
        #region Properties
        private readonly string _name = "no name";
        public string Name { get { return _name; } }

        private readonly string _titleOfFeed = "no feed title";
        public string TitleOfFeed { get { return _titleOfFeed; } }

        private readonly Uri _link = null;
        public Uri Link { get { return _link; } }

        private readonly DateTime _pubDate = DateTime.MinValue;
        public DateTime PubDate { get { return _pubDate; } }

        private bool _unread = true;
        public bool Unread
        {
            get
            {
                return _unread;
            }
            private set
            {
                if (_unread != value)
                {
                    _unread = value;

                    RaisePropertyChanged(nameof(Unread));
                }
            }
        }

        public bool HasEnclosure
        {
            get { return (_enclosure != null); }
        }

        private readonly RdrEnclosure _enclosure = null;
        public RdrEnclosure Enclosure { get { return _enclosure; } }
        #endregion

        public RdrFeedItem(string name, string titleOfFeed)
        {
            _name = name;
            _titleOfFeed = titleOfFeed;
        }

        public RdrFeedItem(XElement e, string titleOfFeed)
        {
            this._titleOfFeed = titleOfFeed;
            string tmpDuration = string.Empty;

            foreach (XElement each in e.Elements())
            {
                if (each.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    string title = String.IsNullOrWhiteSpace(each.Value) ? "no title" : each.Value.RemoveNewLines();

                    title = title.RemoveUnicodeCategories(new List<UnicodeCategory>
                    {
                        UnicodeCategory.OtherSymbol
                    });

                    title = title.Trim();

                    this._name = title;
                }

                if (each.Name.LocalName.Equals("pubDate", StringComparison.OrdinalIgnoreCase))
                {
                    if (this._pubDate == DateTime.MinValue)
                    {
                        this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                    }
                }

                if (each.Name.LocalName.Equals("published", StringComparison.OrdinalIgnoreCase))
                {
                    if (this._pubDate == DateTime.MinValue)
                    {
                        this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                    }
                }

                if (each.Name.LocalName.Equals("updated", StringComparison.OrdinalIgnoreCase))
                {
                    if (this._pubDate == DateTime.MinValue)
                    {
                        this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                    }
                }

                if (each.Name.LocalName.Equals("enclosure", StringComparison.OrdinalIgnoreCase))
                {
                    if (this._enclosure == null)
                    {
                        this._enclosure = new RdrEnclosure(each);
                    }
                }

                if (each.Name.LocalName.Equals("link", StringComparison.OrdinalIgnoreCase))
                {
                    this._link = HelperMethods.ConvertXElementToUri(each);

                    if (this._link == null)
                    {
                        if (each.Attribute("href") != null)
                        {
                            string href = each.Attribute("href").Value;

                            if (String.IsNullOrWhiteSpace(href) == false)
                            {
                                this._link = HelperMethods.ConvertStringToUri(href);
                            }
                        }
                    }

                    if (each.Attribute("rel") != null)
                    {
                        if (each.Attribute("rel").Value.Equals("enclosure"))
                        {
                            if (this._enclosure == null)
                            {
                                this._enclosure = new RdrEnclosure(each);
                            }
                        }
                    }
                }

                if (each.Name.LocalName.Equals("duration", StringComparison.OrdinalIgnoreCase))
                {
                    tmpDuration = each.Value;
                }
            }

            if (_enclosure != null)
            {
                _enclosure.Duration = tmpDuration;
            }
        }

        public void MarkAsRead()
        {
            Unread = false;
        }

        public bool Equals(RdrFeedItem other)
        {
            if (other == null) { throw new ArgumentNullException(nameof(other)); }
            
            if (Name.Equals(other.Name)
                && TitleOfFeed.Equals(other.TitleOfFeed)
                && IsUriSchemeOnlyDifference(Link, other.Link))
            {
                return true;
            }

            return false;
        }

        private static bool IsUriSchemeOnlyDifference(Uri thisLink, Uri otherLink)
        {
            if (thisLink == null && otherLink == null) { return true; }
            if (thisLink == null || otherLink == null) { return false; }

            string thisLinkStr = string.Concat(thisLink.DnsSafeHost, thisLink.PathAndQuery);
            string otherLinkStr = string.Concat(otherLink.DnsSafeHost, otherLink.PathAndQuery);

            if (thisLinkStr.Equals(otherLinkStr))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        
        public int CompareTo(RdrFeedItem other)
        {
            if (other == null) { throw new ArgumentNullException(nameof(other)); }

            if (other.PubDate > PubDate)
            {
                return 1;
            }
            else if (other.PubDate < PubDate)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetType().ToString());
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Name: {0}", Name));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "TitleOfFeed: {0}", TitleOfFeed));

            if (Link == null)
            {
                sb.AppendLine("Link is null");
            }
            else
            {
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Link: {0}", Link.AbsoluteUri));
            }

            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "PubDate: {0}", PubDate.ToString()));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Unread: {0}", Unread.ToString()));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Has enclosure: {0}", HasEnclosure.ToString()));

            if (HasEnclosure)
            {
                sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Enclosure:{0}", Environment.NewLine));
                sb.AppendLine(Enclosure.ToString());
            }

            return sb.ToString();
        }
    }
}
