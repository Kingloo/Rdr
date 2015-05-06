using System;
using System.Text;
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
            if (this.Name.Equals(other.Name, StringComparison.InvariantCultureIgnoreCase) == false)
            {
                return false;
            }

            if (this.Link != null)
            {
                //if (this.Link.AbsoluteUri.Equals(other.Link.AbsoluteUri) == false)
                //{
                //    return false;
                //}

                // Re/code was alternating between http and https links - we are unconcerned about that
                // hence .PathAndQuery which omits the scheme
                if (this.Link.PathAndQuery.Equals(other.Link.PathAndQuery) == false)
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

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("Name: {0}", this.Name));
            sb.AppendLine(string.Format("TitleOfFeed: {0}", this.TitleOfFeed));

            if (this.Link != null)
            {
                sb.AppendLine(string.Format("Link: {0}", this.Link.AbsoluteUri));
            }
            else
            {
                sb.AppendLine("Link is null");
            }

            sb.AppendLine(string.Format("PubDate: {0}", this.PubDate.ToString()));
            sb.AppendLine(string.Format("Unread: {0}", this.Unread.ToString()));
            sb.AppendLine(string.Format("Has enclosure: {0}", this.HasEnclosure.ToString()));

            if (this.HasEnclosure)
            {
                sb.AppendLine(string.Format("Enclosure:{0}", Environment.NewLine));
                sb.AppendLine(this.Enclosure.ToString());
            }

            return sb.ToString();
        }
    }
}
