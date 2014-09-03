using System;
using System.Xml;
using System.Xml.Linq;

namespace Rdr
{
    internal static class HelperMethods
    {
        internal enum FeedType { None, Atom, RSS };

        internal static FeedType DetermineFeedType(string websiteAsString)
        {
            XDocument xDoc = null;

            try
            {
                xDoc = XDocument.Parse(websiteAsString);
            }
            catch (XmlException)
            {
                xDoc = null;
            }

            if (xDoc != null)
            {
                if (xDoc.Root.Name.LocalName.Equals("feed"))
                {
                    return FeedType.Atom;
                }
                else if (xDoc.Root.Name.LocalName.Equals("rss"))
                {
                    return FeedType.RSS;
                }
                else
                {
                    return FeedType.None;
                }
            }

            return FeedType.None;
        }

        internal static DateTime TryParseAndTrim(string s)
        {
            DateTime dt = DateTime.MinValue;

            if (DateTime.TryParse(s, out dt))
            {
                return dt;
            }
            else
            {
                if (((s.Length - 1) <= 0))
                {
                    return DateTime.MinValue;
                }

                return TryParseAndTrim(s.Substring(0, s.Length - 1));
            }
        }

        internal static DateTime ConvertXElementToDateTime(XElement each)
        {
            DateTime dt = DateTime.MinValue;

            if (DateTime.TryParse(each.Value, out dt))
            {
                return dt;
            }
            else
            {
                return HelperMethods.TryParseAndTrim(each.Value);
            }
        }

        internal static DateTime ConvertStringToDateTime(string p)
        {
            DateTime dt = DateTime.MinValue;

            if (DateTime.TryParse(p, out dt))
            {
                return dt;
            }
            else
            {
                return HelperMethods.TryParseAndTrim(p);
            }
        }

        internal static Uri ConvertXElementToUri(XElement each)
        {
            Uri uri = null;

            if (Uri.TryCreate(each.Value, UriKind.Absolute, out uri))
            {
                return uri;
            }
            else
            {
                return null;
            }
        }

        internal static Uri ConvertStringToUri(string p)
        {
            Uri uri = null;

            if (Uri.TryCreate(p, UriKind.Absolute, out uri))
            {
                return uri;
            }
            else
            {
                return null;
            }
        }

        internal static int ConvertXElementToInt32(XElement each)
        {
            int i = 0;

            if (Int32.TryParse(each.Value, out i))
            {
                return i;
            }
            else
            {
                return -1;
            }
        }

        internal static int ConvertStringToInt32(string p)
        {
            int i = 0;

            if (Int32.TryParse(p, out i))
            {
                return i;
            }
            else
            {
                return -1;
            }
        }   
    }
}
