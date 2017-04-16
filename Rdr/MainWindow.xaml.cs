using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Rdr.Model;

namespace Rdr
{
    public partial class MainWindow : Window
    {
        private FeedManager feedManager = null;
        
        public MainWindow(FeedManager viewModel)
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            KeyUp += Window_KeyUp;
            Closing += Window_Closing;

            feedManager = viewModel;
            feedManager.FeedChanged += FeedManager_FeedChanged;

            DataContext = feedManager;
        }
        
        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await feedManager.LoadFeedsAsync();
            
            var unreadCollector = feedManager.Feeds.SortFirst;

            SetFeedItemsBinding(unreadCollector);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    break;
                default:
                    break;
            }
        }

        private void FeedManager_FeedChanged(object sender, EventArgs e)
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
            
            sortByDate.SortDescriptions.Add(
                new SortDescription(
                    nameof(RdrFeedItem.PubDate),
                    ListSortDirection.Descending));
            
            Binding itemsBinding = new Binding
            {
                Source = sortByDate,
                Mode = BindingMode.OneWay
            };

            BindingOperations.SetBinding(ic_Items,
                ItemsControl.ItemsSourceProperty,
                itemsBinding);
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
