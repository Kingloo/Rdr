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
        
        public MainWindow()
        {
            InitializeComponent();

            DataContextChanged += MainWindow_DataContextChanged;
            Loaded += MainWindow_Loaded;
            KeyDown += MainWindow_KeyDown;
            Closing += Window_Closing;
        }

        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            feedManager = (FeedManager)e.NewValue;

            feedManager.FeedChanged += FeedManager_FeedChanged;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await feedManager.LoadFeedsAsync();
            
            var unreadCollector = feedManager.Feeds.SortFirst;

            SetFeedItemsBinding(unreadCollector);
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void FeedManager_FeedChanged(object sender, EventArgs e)
        {
            if (sender is RdrFeed feed)
            {
                SetFeedItemsBinding(feed);
            }
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
            MessageBoxResult mbr = MessageBox.Show(
                "Do you really want to Quit?",
                "Quit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (mbr == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
        }
    }
}
