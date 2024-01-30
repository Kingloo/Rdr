using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using RdrLib.Model;

namespace RdrLib.Helpers
{
	internal static class EnclosureHelpers
	{
		internal static Enclosure? Create(XElement element)
		{
			if (GetLink(element) is Uri uri)
			{
				Int64? size = GetSize(element);

				return new Enclosure(uri, size);
			}
			else
			{
				return null;
			}
		}

		private static Uri? GetLink(XElement element)
		{
			IEnumerable<XAttribute> linkAttributes = element
				.Attributes()
				.Where(static x => x.Name.LocalName.Equals("url", StringComparison.OrdinalIgnoreCase) || x.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase));

			foreach (XAttribute each in linkAttributes)
			{
				if (Uri.TryCreate(each.Value, UriKind.Absolute, out Uri? uri))
				{
					return uri;
				}
			}

			return null;
		}

		private static Int64? GetSize(XElement element)
		{
			IEnumerable<XAttribute> lengthAttributes = element
				.Attributes()
				.Where(static x => x.Name.LocalName.Equals("length", StringComparison.OrdinalIgnoreCase));

			foreach (XAttribute each in lengthAttributes)
			{
				if (Int64.TryParse(each.Value, out Int64 length))
				{
					return length;
				}
			}

			return null;
		}
	}
}
