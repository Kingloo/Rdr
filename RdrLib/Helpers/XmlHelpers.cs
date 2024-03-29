using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Linq;

namespace RdrLib.Helpers
{
	internal static class XmlHelpers
	{
		internal static bool TryParse(string raw, [NotNullWhen(true)] out XDocument? document)
		{
			try
			{
				document = XDocument.Parse(raw.TrimStart()); // parsing fails if there is any leading whitespace
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
			var oic = StringComparison.OrdinalIgnoreCase;

			if (document.Root?.Name.LocalName.Equals("feed", oic) ?? false)
			{
				return FeedType.Atom;
			}
			else if (document.Root?.Name.LocalName.Equals("rss", oic) ?? false)
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
