using System;
using System.Collections.ObjectModel;

namespace Rdr.Fidr
{
    interface IFeed
    {
        string Name { get; set; }
        Uri XmlUrl { get; }
        string Generator { get; }
        DateTime LastBuildDate { get; set; }
        bool Updating { get; set; }
        string Tooltip { get; }
        IFeedImage Image { get; }
        ObservableCollection<IFeedItem> FeedItems { get; }
        
        void Load(string xmlAsString);
        void MarkAllItemsAsRead();
    }
}
