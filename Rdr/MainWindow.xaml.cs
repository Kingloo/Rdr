using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Rdr
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            feedManager.FeedChanged += feedManager_FeedChanged;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RdrFeed unreadCollector = feedManager.Feeds[0];

            SetFeedItemsBinding(unreadCollector);
        }

        private void feedManager_FeedChanged(object sender, EventArgs e)
        {
            RdrFeed feed = (RdrFeed)sender;

            SetFeedItemsBinding(feed);
        }

        private void SetFeedItemsBinding(RdrFeed feed)
        {
            CollectionViewSource sortByDate = new CollectionViewSource
            {
                IsLiveSortingRequested = true,
                Source = feed.Items
            };

            sortByDate.SortDescriptions.Add(new SortDescription("PubDate", ListSortDirection.Descending));

            Binding itemsBinding = new Binding
            {
                Source = sortByDate,
                Mode = BindingMode.OneWay
            };

            BindingOperations.SetBinding(ic_Items, ItemsControl.ItemsSourceProperty, itemsBinding);
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F1)
            {
                StringBuilder sb = new StringBuilder();

                foreach (RdrFeedItem each in feedManager.Items)
                {
                    sb.AppendLine(each.ToString());
                }

                Utils.LogMessage(sb.ToString());
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MessageBoxResult mbr = MessageBox.Show("Do you really want to Quit?", "Quit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            switch (mbr)
            {
                case MessageBoxResult.Yes:
                    e.Cancel = false;
                    break;
                case MessageBoxResult.No:
                    e.Cancel = true;
                    break;
                default:
                    e.Cancel = true;
                    break;
            }
        }
    }
}
