using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using RdrLib.Extensions;
using RdrLib.Model;

namespace RdrLib.Helpers
{
	internal static class ItemHelpers
	{
		internal static IReadOnlyCollection<Item> CreateItems(IEnumerable<XElement?> elements, string feedTitle)
		{
			List<Item> items = new List<Item>();

			foreach (XElement? element in elements)
			{
				if (element is null)
				{
					continue;
				}

				Uri? link = GetLink(element);
				string name = GetName(element);
				DateTimeOffset published = GetPublished(element);
				Enclosure? enclosure = GetEnclosure(element);

				Item item = new Item(feedTitle)
				{
					Link = link,
					Name = name,
					Published = published,
					Enclosure = enclosure
				};

				items.Add(item);
			}

			return items.AsReadOnly();
		}

		private static Uri? GetLink(XElement? element)
		{
			XElement? linkElement = element?.Elements().Where(x => x.Name.LocalName.Equals("link", StringComparison.Ordinal)).FirstOrDefault();

			Uri? uri = null;

			if (linkElement != null)
			{
				if (!Uri.TryCreate(linkElement.Value, UriKind.Absolute, out uri))
				{
					if (linkElement.Attribute("href") is XAttribute href)
					{
						Uri.TryCreate(href.Value, UriKind.Absolute, out uri);
					}
				}
			}

			return uri;
		}

		private static string GetName(XElement? element)
		{
			XElement? titleElement = element?.Elements().Where(x => x.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

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

		private static DateTimeOffset GetPublished(XElement? element)
		{
			IEnumerable<XElement?> allPubDateElements = element?.Elements().Where(x => IsByAnyPubDateName(x)) ?? Enumerable.Empty<XElement>();

			foreach (XElement? pubDateElement in allPubDateElements)
			{
				if (pubDateElement is null)
				{
					continue;
				}

				if (DateTimeOffset.TryParse(pubDateElement.Value, out DateTimeOffset dto))
				{
					return dto;
				}
				else
				{
					// some sites, such as AnandTech, publish datetime in a bad format
					// e.g. Thu, 12 Dec 2019 11:00:00 EDT
					// if we remove the "EDT" it parses correctly

					int end = pubDateElement.Value.Length - 4;

					if (end > 0)
					{
						string valueWithoutTimeZone = pubDateElement.Value.Substring(0, end);

						if (DateTimeOffset.TryParse(valueWithoutTimeZone, out DateTimeOffset fixedDto))
						{
							return fixedDto;
						}
					}
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

		private static Enclosure? GetEnclosure(XElement element)
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
