using System;
using System.Globalization;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class RSSFeedItem : FeedItem
    {
        public RSSFeedItem(XElement x, string titleOfFeed)
        {
            this.TitleOfFeed = titleOfFeed;
            
            // we need to "cache" the Duration value outside the foreach loop
            // if duration appears before enclosure, there will be no enclosure object yet
            // duration set after foreach
            string tmpDuration = string.Empty;

            foreach (XElement each in x.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    this.Name = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value.RemoveNewLines();
                }

                if (each.Name.LocalName.Equals("description"))
                {
                    this.Description = String.IsNullOrEmpty(each.Value) ? "no description" : each.Value;
                }

                if (each.Name.LocalName.Equals("author"))
                {
                    this.Author = String.IsNullOrEmpty(each.Value) ? "no author" : each.Value;
                }

                if (each.Name.LocalName.Equals("pubDate", StringComparison.InvariantCultureIgnoreCase))
                {
                    this.PubDate = HelperMethods.ConvertXElementToDateTime(each);
                }

                if (each.Name.LocalName.Equals("link"))
                {
                    this.Link = HelperMethods.ConvertXElementToUri(each);
                }

                if (each.Name.LocalName.Equals("enclosure"))
                {
                    this.Enclosure = new RSSFeedEnclosure(each);
                }

                if (each.Name.LocalName.Equals("duration"))
                {
                    tmpDuration = String.IsNullOrEmpty(each.Value) ? string.Empty : each.Value;
                }
            }

            if (this.Enclosure != null)
            {
                this.HasEnclosure = true;
                this.Enclosure.Duration = string.IsNullOrEmpty(tmpDuration) ? "00:00:00" : tmpDuration;
            }
        }

        public static bool TryCreate(XElement x, string titleOfFeed, out RSSFeedItem feedItem)
        {
            if (x.IsEmpty)
            {
                feedItem = null;
                return false;
            }

            if (x.HasElements == false)
            {
                feedItem = null;
                return false;
            }

            feedItem = new RSSFeedItem(x, titleOfFeed);

            if (feedItem == null)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
