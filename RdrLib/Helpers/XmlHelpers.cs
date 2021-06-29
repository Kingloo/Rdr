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
			var oic = StringComparison.OrdinalIgnoreCase;

			if (document.Root.Name.LocalName.Equals("feed", oic))
			{
				return FeedType.Atom;
			}
			else if (document.Root.Name.LocalName.Equals("rss", oic))
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
