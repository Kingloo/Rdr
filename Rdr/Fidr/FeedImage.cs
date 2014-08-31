using System;

namespace Rdr.Fidr
{
    abstract class FeedImage
    {
        protected Uri _uri = null;
        public Uri Uri { get { return this._uri; } }

        protected string _title = string.Empty;
        public string Title { get { return this._title; } }
    }
}
