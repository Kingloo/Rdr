using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using RdrLib.Extensions;
using RdrLib.Model;

namespace RdrLib.Helpers
{
    public static class ItemHelpers
    {
        public static IEnumerable<Item> CreateItems(IEnumerable<XElement> elements)
        {
            if (elements is null) { throw new ArgumentNullException(nameof(elements)); }

            Collection<Item> items = new Collection<Item>();

            foreach (XElement element in elements)
            {
                Uri link = GetLink(element);
                string name = GetName(element);
                DateTimeOffset published = GetPublished(element);
                Enclosure enclosure = GetEnclosure(element);

                Item item = new Item
                {
                    Link = link,
                    Name = name,
                    Published = published,
                    Enclosure = enclosure
                };

                items.Add(item);
            }

            return items;
        }

        private static Uri GetLink(XElement element)
        {
            XElement linkElement = element.Elements().Where(x => x.Name.LocalName.Equals("link", StringComparison.Ordinal)).FirstOrDefault();

            Uri uri = null;

            if (linkElement != null)
            {
                if (!Uri.TryCreate(linkElement.Value, UriKind.Absolute, out uri))
                {
                    if (element.Attribute("href") is XAttribute href)
                    {
                        Uri.TryCreate(href.Value, UriKind.Absolute, out uri);
                    }
                }
            }

            return uri;
        }

        private static string GetName(XElement element)
        {
            XElement titleElement = element
                .Elements()
                .Where(x => x.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();

            if (titleElement != null)
            {
                if (!String.IsNullOrWhiteSpace(titleElement.Value))
                {
                    return titleElement
                        .Value
                        .RemoveUnicodeCategories(new[] { UnicodeCategory.OtherSymbol })
                        .Trim();
                }
            }

            return "title not found";
        }

        private static DateTimeOffset GetPublished(XElement element)
        {
            IEnumerable<XElement> allPubDateElements = element.Elements().Where(x => IsByAnyPubDateName(x));

            foreach (XElement pubDateElement in allPubDateElements)
            {
                if (DateTimeOffset.TryParse(pubDateElement.Value, out DateTimeOffset dto))
                {
                    return dto;
                }
            }

            return DateTimeOffset.MinValue;
        }

        private static bool IsByAnyPubDateName(XElement element)
        {
            string localName = element.Name.LocalName;

            return localName.Equals("pubDate", StringComparison.Ordinal)
                || localName.Equals("published", StringComparison.OrdinalIgnoreCase)
                || localName.Equals("updated", StringComparison.OrdinalIgnoreCase);
        }

        private static Enclosure GetEnclosure(XElement element)
        {
            IEnumerable<XElement> enclosureElements = element
                .Elements()
                .Where(
                    x => x.Name.LocalName.Equals("enclosure", StringComparison.OrdinalIgnoreCase)
                    || x.Attributes("rel").Any());

            foreach (XElement each in enclosureElements)
            {
                string localName = each.Name.LocalName;

                if (localName.Equals("enclosure", StringComparison.OrdinalIgnoreCase))
                {
                    return EnclosureHelpers.Create(each);
                }
                else if (localName.Equals("link", StringComparison.OrdinalIgnoreCase))
                {
                    if (each.Attribute("rel") is XAttribute rel)
                    {
                        if (rel.Value.Equals("enclosure", StringComparison.OrdinalIgnoreCase))
                        {
                            return EnclosureHelpers.Create(each);
                        }
                    }
                }
            }

            return null;
        }
    }
}
