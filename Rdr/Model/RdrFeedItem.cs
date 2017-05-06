using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Rdr.Extensions;

namespace Rdr.Model
{
    public class RdrFeedItem : ViewModelBase, IEquatable<RdrFeedItem>, IComparable<RdrFeedItem>
    {
        #region Properties
        private readonly string _name = "no name";
        public string Name => _name;

        private readonly string _titleOfFeed = "no feed title";
        public string TitleOfFeed => _titleOfFeed;

        private readonly Uri _link = null;
        public Uri Link => _link;

        private readonly DateTime _pubDate = DateTime.MinValue;
        public DateTime PubDate => _pubDate;

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

        public bool HasEnclosure => _enclosure != null;

        private readonly RdrEnclosure _enclosure = null;
        public RdrEnclosure Enclosure => _enclosure;
        #endregion

        public RdrFeedItem(string name, string titleOfFeed)
        {
            _name = name;
            _titleOfFeed = titleOfFeed;
        }

        public RdrFeedItem(XElement element, string titleOfFeed)
        {
            _titleOfFeed = titleOfFeed;

            _name = GetName(element.Elements()
                .Where(x => x.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault());
            
            _pubDate = GetPubDate(from each in element.Elements()
                                  where each.Name.LocalName.Equals("pubDate", StringComparison.OrdinalIgnoreCase)
                                  || each.Name.LocalName.Equals("published", StringComparison.OrdinalIgnoreCase)
                                  || each.Name.LocalName.Equals("updated", StringComparison.OrdinalIgnoreCase)
                                  select each);
            
            _enclosure = GetEnclosure(from each in element.Elements()
                                      where each.Name.LocalName.Equals("enclosure")
                                      || each.Attributes("rel").Any()
                                      select each);

            _link = GetLink(element.Elements()
                .Where(x => x.Name.LocalName.Equals("link", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault());
            
            if (_enclosure != null)
            {
                _enclosure.Duration = GetDuration(element.Elements()
                    .Where(x => x.Name.LocalName.Equals("duration"))
                    .FirstOrDefault());
            }
        }
        
        private static string GetName(XElement element)
        {
            string ifAbsent = "no title";

            if (element == default(XElement)) { return ifAbsent; }

            return String.IsNullOrWhiteSpace(element.Value)
                ? ifAbsent
                : element
                .Value
                .RemoveUnicodeCategories(new List<UnicodeCategory>
                {
                    UnicodeCategory.OtherSymbol
                })
                .Trim();
        }

        private static DateTime GetPubDate(IEnumerable<XElement> elements)
        {
            DateTime dt = DateTime.MinValue;
            
            foreach (var each in elements)
            {
                // chops a character off the end and tries again
                // can make it work when pubDate ends in e.g. " PDT"
                dt = TryParseRecurse(each.Value);

                if (dt > DateTime.MinValue)
                {
                    break;
                }
            }

            return dt;
        }
        
        private static DateTime TryParseRecurse(string value)
        {
            DateTime dt = DateTime.Now;

            if (!DateTime.TryParse(value, out dt))
            {
                int oneCharShorter = value.Length - 1;

                if (oneCharShorter <= 0)
                {
                    return dt;
                }

                return TryParseRecurse(value.Substring(0, oneCharShorter));
            }

            return dt;
        }

        private RdrEnclosure GetEnclosure(IEnumerable<XElement> elements)
        {
            foreach (var each in elements)
            {
                if (each == default(XElement))
                {
                    continue;
                }

                if (each.Name.LocalName.Equals("enclosure"))
                {
                    return new RdrEnclosure(this, each);
                }
                else if (each.Name.LocalName.Equals("link"))
                {
                    if (each.Attribute("rel").Value.Equals("enclosure"))
                    {
                        return new RdrEnclosure(this, each);
                    }
                }
            }

            return null;
        }

        private static Uri GetLink(XElement element)
        {
            if (element == default(XElement)) { return null; }

            if (!Uri.TryCreate(element.Value, UriKind.Absolute, out Uri uri))
            {
                if (element.Attribute("href") != null)
                {
                    string href = element.Attribute("href").Value;

                    if (!String.IsNullOrWhiteSpace(href))
                    {
                        if (Uri.TryCreate(href, UriKind.Absolute, out Uri hrefUri))
                        {
                            uri = hrefUri;
                        }
                    }
                }
            }

            return uri;
        }

        private static string GetDuration(XElement element)
        {
            string ifAbsent = "no duration";

            if (element == default(XElement)) { return ifAbsent; }

            return String.IsNullOrWhiteSpace(element.Value)
                ? ifAbsent
                : element.Value.Trim();
        }

        public void MarkAsRead() => Unread = false;

        public bool Equals(RdrFeedItem other)
        {
            if (other == null) { return false; }

            bool sameName = Name.Equals(other.Name);
            bool sameFeedTitle = TitleOfFeed.Equals(other.TitleOfFeed);
            bool sameLink = AreLinksTheSame(Link, other.Link);

            return sameName && sameFeedTitle && sameLink;
        }

        private static bool AreLinksTheSame(Uri me, Uri other)
        {
            if (me == null && other == null) { return true; }
            
            if ((me == null) != (other == null))
            {
                return false;
            }

            return me.AbsolutePath.Equals(other.AbsolutePath);
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

            sb.AppendLine(GetType().Name);
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
