using System;
using System.Net.Mime;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class RSSFeedEnclosure : FeedEnclosure
    {
        public RSSFeedEnclosure(XElement x)
        {
            foreach (XAttribute each in x.Attributes())
            {
                if (each.Name.LocalName.Equals("type"))
                {
                    this._contentType = new ContentType { MediaType = (String.IsNullOrEmpty(each.Value) ? "undefined" : each.Value) };
                }

                if (each.Name.LocalName.Equals("url"))
                {
                    this._link = HelperMethods.ConvertStringToUri(each.Value);
                }

                if (each.Name.LocalName.Equals("length"))
                {
                    this._fileSize = HelperMethods.ConvertStringToInt32(each.Value);
                }
            }
        }

        public static bool TryCreate(XElement x, out RSSFeedEnclosure rssFeedEnclosure)
        {
            if (x.IsEmpty)
            {
                rssFeedEnclosure = null;
                return false;
            }

            if (x.HasAttributes == false)
            {
                rssFeedEnclosure = null;
                return false;
            }

            rssFeedEnclosure = new RSSFeedEnclosure(x);

            if (rssFeedEnclosure == null)
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
