using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class AtomFeed : Feed
    {
        public AtomFeed(string websiteAsString, Uri xmlUrl)
        {
            this._xmlUrl = xmlUrl;

            XDocument xDoc = XDocument.Parse(websiteAsString);
            XElement x = xDoc.Root;

            foreach (XElement each in x.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    this.Name = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value;
                }

                if (each.Name.LocalName.Equals("icon"))
                {
                    //this.Image = new AtomFeedImage(each) as FeedImage;
                    this._image = new AtomFeedImage(each);
                }

                if (each.Name.LocalName.Equals("updated", StringComparison.InvariantCultureIgnoreCase))
                {
                    this._lastBuildDate = HelperMethods.ConvertStringToDateTime(each.Value);
                }

                if (each.Name.LocalName.Equals("generator"))
                {
                    this._generator = String.IsNullOrEmpty(each.Value) ? "none" : each.Value;
                }

                if (each.Name.LocalName.Equals("entry"))
                {
                    AtomFeedEntry entry = null;
                    if (AtomFeedEntry.TryCreate(each, this.Name, out entry))
                    {
                        this.FeedItems.Add(entry);
                    }
                }
            }
        }

        public static bool TryCreate(string websiteAsString, Uri uri, out Feed feed)
        {
            XDocument xDoc = XDocument.Parse(websiteAsString);

            if (xDoc.Root.IsEmpty)
            {
                feed = null;
                return false;
            }

            if (xDoc.Root.HasElements == false)
            {
                feed = null;
                return false;
            }

            feed = new AtomFeed(websiteAsString, uri);

            if (feed == null)
            {
                return false;
            }

            return true;
        }

        public override void Load(string s)
        {
            XDocument xDoc = null;

            try
            {
                xDoc = XDocument.Parse(s);
            }
            catch (XmlException)
            {
                return;
            }

            if (xDoc != null)
            {
                AtomFeedEntry entry = null;

                IEnumerable<AtomFeedEntry> entries = from each in xDoc.Root.Elements("entry")
                                                     where AtomFeedEntry.TryCreate(each, this.Name, out entry)
                                                     select new AtomFeedEntry(each, this.Name);

                this.FeedItems.AddMissingItems<FeedItem>(entries);
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
