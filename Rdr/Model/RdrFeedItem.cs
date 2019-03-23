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

        private readonly Uri _link = default;
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

        private readonly RdrEnclosure _enclosure = default;
        public RdrEnclosure Enclosure => _enclosure;
        #endregion

        public RdrFeedItem(string name, string titleOfFeed)
        {
            _name = name;
            _titleOfFeed = titleOfFeed;
        }

        public RdrFeedItem(XElement element, string titleOfFeed)
        {
            if (element == null) { throw new ArgumentNullException(nameof(element)); }

            var sc = StringComparison.OrdinalIgnoreCase;

            _titleOfFeed = titleOfFeed;

            _name = GetName(element.Elements()
                .Where(x => x.Name.LocalName.Equals("title", sc))
                .FirstOrDefault());
            
            _pubDate = GetPubDate(from each in element.Elements()
                                  let local = each.Name.LocalName
                                  where local.Equals("pubDate", sc)
                                  || local.Equals("published", sc)
                                  || local.Equals("updated", sc)
                                  select each);
            
            _enclosure = GetEnclosure(from each in element.Elements()
                                      where each.Name.LocalName.Equals("enclosure")
                                      || each.Attributes("rel").Any()
                                      select each);

            _link = GetLink(element.Elements()
                .Where(x => x.Name.LocalName.Equals("link", sc))
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
            foreach (XElement each in elements.Where(x => x != default(XElement)))
            {
                if (each.Name.LocalName.Equals("enclosure"))
                {
                    return new RdrEnclosure(this, each);
                }
                else if (each.Name.LocalName.Equals("link"))
                {
                    if (each.Attribute("rel") is XAttribute rel)
                    {
                        if (rel.Value.Equals("enclosure"))
                        {
                            return new RdrEnclosure(this, each);
                        }
                    }
                }
            }

            return null;
        }

        private static Uri GetLink(XElement element)
        {
            Uri uri = null;

            if (element != null)
            {
                if (!Uri.TryCreate(element.Value, UriKind.Absolute, out uri))
                {
                    if (element.Attribute("href") is XAttribute href)
                    {
                        Uri.TryCreate(href.Value, UriKind.Absolute, out uri);
                    }
                }
            }

            return uri;
        }

        private static string GetDuration(XElement element)
        {
            if (element != null)
            {
                if (!String.IsNullOrWhiteSpace(element.Value))
                {
                    return element.Value.Trim();
                }
            }

            return "no duration";
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

            var cc = CultureInfo.CurrentCulture;

            sb.AppendLine(GetType().FullName);
            sb.AppendLine(string.Format(cc, "Name: {0}", Name));
            sb.AppendLine(string.Format(cc, "TitleOfFeed: {0}", TitleOfFeed));

            if (Link == null)
            {
                sb.AppendLine("Link is null");
            }
            else
            {
                sb.AppendLine(string.Format(cc, "Link: {0}", Link.AbsoluteUri));
            }

            sb.AppendLine(string.Format(cc, "PubDate: {0}", PubDate.ToString(cc)));
            sb.AppendLine(string.Format(cc, "Unread: {0}", Unread.ToString()));
            sb.AppendLine(string.Format(cc, "Has enclosure: {0}", HasEnclosure.ToString()));

            if (HasEnclosure)
            {
                sb.AppendLine(string.Format(cc, "Enclosure:{0}", Environment.NewLine));
                sb.AppendLine(Enclosure.ToString());
            }

            return sb.ToString();
        }
    }
}
