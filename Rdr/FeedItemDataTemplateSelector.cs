using System.Windows;
using System.Windows.Controls;

namespace Rdr
{
    public class FeedItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate WithEnclosure { get; set; }
        public DataTemplate NoEnclosure { get; set; }
        
        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            RdrFeedItem feedItem = (RdrFeedItem)item;

            if (feedItem.HasEnclosure)
            {
                return WithEnclosure;
            }
            else
            {
                return NoEnclosure;
            }
        }
    }
}
