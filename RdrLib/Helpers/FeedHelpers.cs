using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using RdrLib.Extensions;
using RdrLib.Model;

namespace RdrLib.Helpers
{
    public static class FeedHelpers
    {
        public static bool TryCreate(Uri uri, out Feed feed)
        {
            if (uri is null) { throw new ArgumentNullException(nameof(uri)); }

            if (!uri.IsAbsoluteUri)
            {
                feed = null;
                return false;
            }

            feed = new Feed(uri);
            return true;
        }

        public static string GetName(XDocument document)
        {
            if (document is null) { throw new ArgumentNullException(nameof(document)); }

            FeedType feedType = XmlHelpers.DetermineFeedType(document);

            switch (feedType)
            {
                case FeedType.Atom:
                    return GetName(document.Root);
                case FeedType.RSS:
                    return GetName(document.Root.Element("channel"));
                default:
                    return string.Empty;
            }
        }

        private static string GetName(XElement element)
        {
            return element
                .Elements()
                .Where(x => x.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(x.Value))
                .FirstOrDefault()
                ?.Value
                .RemoveUnicodeCategories(new[] { UnicodeCategory.OtherSymbol })
                .Trim()
                ??
                "unknown title";
        }

        public static IEnumerable<Item> GetItems(XDocument document)
        {
            if (document is null) { throw new ArgumentNullException(nameof(document)); }

            FeedType feedType = XmlHelpers.DetermineFeedType(document);

            switch (feedType)
            {
                case FeedType.Atom:
                    return ItemHelpers.CreateItems(document.Root.Elements(XName.Get("entry", "http://www.w3.org/2005/Atom")));
                case FeedType.RSS:
                    return ItemHelpers.CreateItems(document.Root.Element("channel").Elements("item"));
                default:
                    return Enumerable.Empty<Item>();
            }
        }
    }
}
