using System;
using System.Windows;
using System.Windows.Controls;

namespace Rdr
{
    public partial class ItemUC : UserControl
    {
        private FeedItem feedItemDataContext = null;

        public ItemUC()
        {
            this.InitializeComponent();

            this.DataContextChanged += ItemUC_DataContextChanged;
        }

        private void ItemUC_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(this.DataContext is FeedItem))
            {
                throw new ArgumentException("ItemUC.xaml.cs -> ItemUC_DataContextChanged -> object sender was not of required type Rdr.FeedItem");
            }
            
            feedItemDataContext = this.DataContext as FeedItem;
            feedItemDataContext.PropertyChanged += feedItemDataContext_PropertyChanged;
        }

        private void feedItemDataContext_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Unread"))
            {
                //VisualStateManager.GoToState(this, "Read", true);
                Console.WriteLine("fishsticks");
            }
        }
    }
}