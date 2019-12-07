using System;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;

namespace RdrLib.Helpers
{
    internal static class XmlHelpers
    {
        internal static bool TryParse(string raw, out XDocument? document)
        {
            try
            {
                document = XDocument.Parse(raw);
                return true;
            }
            catch (XmlException)
            {
                document = null;
                return false;
            }
        }

        internal static FeedType DetermineFeedType(XDocument document)
        {
            var sco = StringComparison.OrdinalIgnoreCase;

            if (document.Root.Name.LocalName.Equals("feed", sco))
            {
                return FeedType.Atom;
            }
            else if (document.Root.Name.LocalName.Equals("rss", sco))
            {
                return FeedType.RSS;
            }
            else
            {
                return FeedType.Unknown;
            }
        }
    }
}
