using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using RdrLib.Extensions;
using RdrLib.Model;

namespace RdrLib.Helpers
{
	internal static class FeedHelpers
	{
		internal static bool TryCreate(Uri uri, [NotNullWhen(true)] out Feed? feed)
		{
			if (!uri.IsAbsoluteUri)
			{
				feed = null;
				return false;
			}

			feed = new Feed(uri);
			return true;
		}

		internal static string GetName(XDocument document)
		{
			FeedType feedType = XmlHelpers.DetermineFeedType(document);

			return feedType switch
			{
				FeedType.Atom => GetName(document.Root),
				FeedType.RSS => GetName(document.Root?.Element("channel")),
				_ => string.Empty,
			};
		}

		private static string GetName(XElement? element)
		{
			return element
				?.Elements()
				.Where(x => x.Name.LocalName.Equals("title", StringComparison.OrdinalIgnoreCase) && !String.IsNullOrEmpty(x.Value))
				.FirstOrDefault()
				?.Value
				.RemoveUnicodeCategories(new[] { UnicodeCategory.OtherSymbol })
				.Trim()
				??
				"unknown title";
		}

		internal static IReadOnlyCollection<Item> GetItems(XDocument document, string feedName)
		{
			FeedType feedType = XmlHelpers.DetermineFeedType(document);

			return feedType switch
			{
				FeedType.Atom => ItemHelpers.CreateItems(document.Root?.Elements(XName.Get("entry", "http://www.w3.org/2005/Atom")) ?? Enumerable.Empty<XElement>(), feedName),
				FeedType.RSS => ItemHelpers.CreateItems(document.Root?.Element("channel")?.Elements("item") ?? Enumerable.Empty<XElement>(), feedName),
				_ => new List<Item>(0).AsReadOnly()
			};
		}
	}
}
