using System.Windows;
using System.Windows.Controls;
using Rdr.Fidr;

namespace Rdr
{
    public class FeedItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate WithEnclosure { get; set; }
        public DataTemplate NoEnclosure { get; set; }
        
        public override DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
        {
            IFeedItem feedItem = (IFeedItem)item;

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
