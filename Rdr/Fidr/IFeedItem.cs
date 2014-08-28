using System;

namespace Rdr.Fidr
{
    interface IFeedItem : IEquatable<IFeedItem>
    {
        string Name { get; set; }
        string TitleOfFeed { get; set; }
        bool Unread { get; set; }
        string Description { get; set; }
        string Author { get; set; }
        DateTime PubDate { get; set; }
        Uri Link { get; set; }
        IFeedEnclosure Enclosure { get; set; }
        bool HasEnclosure { get; set; }
        
        void MarkAsRead();
    }
}
