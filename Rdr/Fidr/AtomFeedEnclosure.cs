using System;
using System.Net.Mime;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class AtomFeedEnclosure : FeedEnclosure
    {
        public AtomFeedEnclosure(XElement x)
        {
            foreach (XAttribute each in x.Attributes())
            {
                if (each.Name.LocalName.Equals("type"))
                {
                    this._contentType = new ContentType { MediaType = (String.IsNullOrEmpty(each.Value) ? "undefined" : each.Value) };
                }

                if (each.Name.LocalName.Equals("href"))
                {
                    this._link = HelperMethods.ConvertStringToUri(each.Value);
                }
            }
        }

        public static bool TryCreate(XElement x, out AtomFeedEnclosure atomFeedEnclosure)
        {
            if (x.Attribute("href") == null)
            {
                atomFeedEnclosure = null;
                return false;
            }

            string href = x.Attribute("href").Value;

            Uri tmp = null;
            if (Uri.TryCreate(href, UriKind.Absolute, out tmp))
            {
                atomFeedEnclosure = new AtomFeedEnclosure(x);

                if (atomFeedEnclosure == null)
                {
                    return false;
                }

                return true;
            }
            else
            {
                atomFeedEnclosure = null;
                return false;
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
