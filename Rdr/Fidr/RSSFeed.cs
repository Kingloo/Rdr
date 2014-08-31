using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class RSSFeed : Feed
    {
        private string _description = string.Empty;
        public string Description { get { return this._description; } }

        private Uri _docs = null;
        public Uri Docs { get { return this._docs; } }

        private string _language = string.Empty;
        public string Language { get { return this._language; } }

        private string _webmaster = string.Empty;
        public string Webmaster { get { return this._webmaster; } }

        private int _ttl = 0;
        public int Ttl { get { return this._ttl; } }

        public RSSFeed(string websiteAsString, Uri xmlUrl)
        {
            this._xmlUrl = xmlUrl;
            
            XDocument xDoc = XDocument.Parse(websiteAsString);
            XElement x = xDoc.Root.Element("channel");
            
            foreach (XElement each in x.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    this.Name = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value;
                }

                if (each.Name.LocalName.Equals("description"))
                {
                    this._description = String.IsNullOrEmpty(each.Value) ? "no description" : each.Value;
                }

                if (each.Name.LocalName.Equals("generator"))
                {
                    this._generator = String.IsNullOrEmpty(each.Value) ? "no generator" : each.Value;
                }

                if (each.Name.LocalName.Equals("docs"))
                {
                    this._docs = HelperMethods.ConvertXElementToUri(each);
                }

                if (each.Name.LocalName.Equals("language"))
                {
                    this._language = String.IsNullOrEmpty(each.Value) ? "no language" : each.Value;
                }

                if (each.Name.LocalName.Equals("webmaster", StringComparison.InvariantCultureIgnoreCase))
                {
                    this._webmaster = String.IsNullOrEmpty(each.Value) ? "no webmaster" : each.Value;
                }

                if (each.Name.LocalName.Equals("lastBuildDate", StringComparison.InvariantCultureIgnoreCase))
                {
                    this._lastBuildDate = HelperMethods.ConvertXElementToDateTime(each);
                }

                if (each.Name.LocalName.Equals("ttl"))
                {
                    this._ttl = HelperMethods.ConvertXElementToInt32(each);
                }

                if (each.Name.LocalName.Equals("image"))
                {
                    if ((each.IsEmpty == false) && (each.HasElements))
                    {
                        //this.Image = new RSSFeedImage(each) as IFeedImage;
                        this._image = new RSSFeedImage(each);
                    }
                }

                if (each.Name.LocalName.Equals("item"))
                {
                    RSSFeedItem item = null;
                    if (RSSFeedItem.TryCreate(each, this.Name, out item))
                    {
                        this.FeedItems.Add(item);
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

            if (xDoc.Root.Element("channel") == null)
            {
                feed = null;
                return false;
            }

            XElement channel = xDoc.Root.Element("channel");

            if (channel.IsEmpty)
            {
                feed = null;
                return false;
            }

            if (channel.HasElements == false)
            {
                feed = null;
                return false;
            }

            feed = new RSSFeed(websiteAsString, uri);

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
                RSSFeedItem feedItem = null;

                IEnumerable<FeedItem> feedItems = from each in xDoc.Root.Element("channel").Elements("item")
                                                   where RSSFeedItem.TryCreate(each, this.Name, out feedItem)
                                                   select new RSSFeedItem(each, this.Name);

                this.FeedItems.AddMissingItems<FeedItem>(feedItems);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToString());
            sb.AppendLine(this._docs == null ? "Docs: none" : string.Format("Docs: {0}", this._docs.AbsoluteUri));
            sb.AppendLine(String.IsNullOrEmpty(this._language) ? "Language: none" : string.Format("Language: {0}", this._language));
            sb.AppendLine(String.IsNullOrEmpty(this._webmaster) ? "Webmaster: none" : string.Format("Webmaster: {0}", this._webmaster));
            sb.AppendLine(string.Format("Ttl: {0}", this._ttl.ToString()));

            return sb.ToString();
        }
    }
}
