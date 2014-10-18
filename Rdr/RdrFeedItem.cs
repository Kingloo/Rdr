using System;
using System.Xml.Linq;

namespace Rdr
{
    class RdrFeedItem : RdrBase, IEquatable<RdrFeedItem>, IComparable<RdrFeedItem>
    {
        private readonly string _name = "no name";
        public string Name { get { return this._name; } }

        private readonly string _titleOfFeed = "no feed title";
        public string TitleOfFeed { get { return this._titleOfFeed; } }

        private readonly Uri _link = null;
        public Uri Link { get { return this._link; } }

        private readonly DateTime _pubDate = DateTime.MinValue;
        public DateTime PubDate { get { return this._pubDate; } }

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

        public bool HasEnclosure
        {
            get { return (this._enclosure != null); }
        }

        private readonly RdrEnclosure _enclosure = null;
        public RdrEnclosure Enclosure { get { return this._enclosure; } }

        private readonly string _duration = "00:00:00";
        public string Duration { get { return this._duration; } }

        public RdrFeedItem(XElement e, string titleOfFeed)
        {
            this._titleOfFeed = titleOfFeed;
            string tmpDuration = string.Empty;

            foreach (XElement each in e.Elements())
            {
                if (each.Name.LocalName.Equals("title", StringComparison.InvariantCultureIgnoreCase))
                {
                    this._name = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value.RemoveNewLines();
                }

                if (each.Name.LocalName.Equals("pubDate", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (this._pubDate == DateTime.MinValue)
                    {
                        this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                    }
                }

                if (each.Name.LocalName.Equals("published", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (this._pubDate == DateTime.MinValue)
                    {
                        this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                    }
                }

                if (each.Name.LocalName.Equals("updated", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (this._pubDate == DateTime.MinValue)
                    {
                        this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                    }
                }

                if (each.Name.LocalName.Equals("enclosure", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (this._enclosure == null)
                    {
                        this._enclosure = new RdrEnclosure(each);
                    }
                }

                if (each.Name.LocalName.Equals("link", StringComparison.InvariantCultureIgnoreCase))
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

                if (each.Name.LocalName.Equals("duration", StringComparison.InvariantCultureIgnoreCase))
                {
                    tmpDuration = each.Value;
                }
            }

            if (this._enclosure != null)
            {
                this._enclosure.Duration = tmpDuration;
            }
        }

        public void MarkAsRead()
        {
            this.Unread = false;
        }

        public bool Equals(RdrFeedItem other)
        {
            if (this.Name.Equals(other.Name) == false)
            {
                return false;
            }

            if (this.Link != null)
            {
                if (this.Link.AbsoluteUri.Equals(other.Link.AbsoluteUri) == false)
                {
                    return false;
                }
            }
            else
            {
                if (this.PubDate.Equals(other.PubDate) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public int CompareTo(RdrFeedItem other)
        {
            if (other.PubDate > this.PubDate)
            {
                return 1;
            }
            else if (other.PubDate < this.PubDate)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }
    }
}
