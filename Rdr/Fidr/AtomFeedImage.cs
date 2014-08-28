using System;
using System.Text;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class AtomFeedImage : IFeedImage
    {
        private Uri _uri = null;
        public Uri Uri { get { return this._uri; } }

        public AtomFeedImage(XElement x)
        {
            this._uri = HelperMethods.ConvertStringToUri(x.Value);
        }

        public static bool TryCreate(XElement x, out AtomFeedImage atomFeedImage)
        {
            if (x.IsEmpty)
            {
                atomFeedImage = null;
                return false;
            }

            Uri tmp = null;
            if (Uri.TryCreate(x.Value, UriKind.Absolute, out tmp))
            {
                atomFeedImage = new AtomFeedImage(x);
                return true;
            }

            atomFeedImage = null;
            return false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("Url: {0}", this.Uri.AbsoluteUri));

            return sb.ToString();
        }
    }
}
