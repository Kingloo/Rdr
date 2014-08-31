using System;
using System.Xml.Linq;

namespace Rdr.Fidr
{
    class AtomFeedEntry : FeedItem
    {
        public AtomFeedEntry(XElement x, string titleOfFeed)
        {
            this._titleOfFeed = titleOfFeed;

            foreach (XElement each in x.Elements())
            {
                if (each.Name.LocalName.Equals("title"))
                {
                    this._name = String.IsNullOrEmpty(each.Value) ? "no title" : each.Value.RemoveNewLines();
                }

                if (each.Name.LocalName.Equals("link"))
                {
                    string rel = string.Empty;
                    if (each.Attribute("rel") != null)
                    {
                        rel = each.Attribute("rel").Value;
                    }

                    string href = string.Empty;
                    if (each.Attribute("href") != null)
                    {
                        href = each.Attribute("href").Value;
                    }

                    if (String.IsNullOrEmpty(rel))
                    {
                        if (String.IsNullOrEmpty(href) == false)
                        {
                            this._link = new Uri(href);
                        }
                    }
                    else
                    {
                        switch (rel)
                        {
                            case "alternate":
                                if (String.IsNullOrEmpty(href) == false)
                                {
                                    this._link = new Uri(href);
                                }
                                break;
                            case "enclosure":
                                this._enclosure = new AtomFeedEnclosure(each);
                                this._hasEnclosure = true;
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (each.Name.LocalName.Equals("published", StringComparison.InvariantCultureIgnoreCase))
                {
                    this._pubDate = HelperMethods.ConvertXElementToDateTime(each);
                }

                if (each.Name.LocalName.Equals("content"))
                {
                    this._description = String.IsNullOrEmpty(each.Value) ? "no description" : each.Value;
                }

                if (each.Name.LocalName.Equals("author"))
                {
                    if (each.Element("name") != null)
                    {
                        string authorName = each.Element("name").Value;

                        this._author = String.IsNullOrEmpty(authorName) ? "no author" : authorName;
                    }
                }
            }
        }

        public static bool TryCreate(XElement x, string titleOfFeed, out AtomFeedEntry atomFeedEntry)
        {
            if (x.IsEmpty)
            {
                atomFeedEntry = null;
                return false;
            }

            if (x.HasElements == false)
            {
                atomFeedEntry = null;
                return false;
            }

            atomFeedEntry = new AtomFeedEntry(x, titleOfFeed);

            if (atomFeedEntry == null)
            {
                return false;
            }

            return true;
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
