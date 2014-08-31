using System;
using System.Text;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class RSSFeedImage : FeedImage
    {
        private Uri _link = null;
        public Uri Link { get { return this._link; } }

        private int _height = 0;
        public int Height { get { return this._height; } }

        private int _width = 0;
        public int Width { get { return this._width; } }

        public RSSFeedImage(XElement x)
        {
            foreach (XElement each in x.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    this._title = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value;
                }

                if (each.Name.LocalName.Equals("url"))
                {
                    this._uri = HelperMethods.ConvertStringToUri(each.Value);
                }

                if (each.Name.LocalName.Equals("link"))
                {
                    this._link = HelperMethods.ConvertStringToUri(each.Value);
                }

                if (each.Name.LocalName.Equals("width"))
                {
                    this._width = HelperMethods.ConvertStringToInt32(each.Value);
                }

                if (each.Name.LocalName.Equals("height"))
                {
                    this._height = HelperMethods.ConvertStringToInt32(each.Value);
                }
            }
        }

        public static bool TryCreate(XElement x, out RSSFeedImage rssFeedImage)
        {
            if (x.IsEmpty)
            {
                rssFeedImage = null;
                return false;
            }

            if (x.HasElements == false)
            {
                rssFeedImage = null;
                return false;
            }

            if (x.Element("url") == null)
            {
                rssFeedImage = null;
                return false;
            }
            else
            {
                Uri tmp = null;
                if (Uri.TryCreate(x.Element("url").Value, UriKind.Absolute, out tmp))
                {
                    rssFeedImage = new RSSFeedImage(x);
                    return true;
                }
            }

            rssFeedImage = null;
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("Title: {0}", this._title));
            sb.AppendLine(this.Uri == null ? "Url: none" : string.Format("Url: {0}", this.Uri.AbsoluteUri));
            sb.AppendLine(this._link == null ? "Link: none" : string.Format("Link: {0}", this._link.AbsoluteUri));
            sb.AppendLine(this._height < 0 ? "Height: none" : string.Format("Height: {0}", this._height.ToString()));
            sb.AppendLine(this._width < 0 ? "Width: none" : string.Format("Width: {0}", this._width.ToString()));

            return sb.ToString();
        }
    }
}
