using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Rdr
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.feedManager.ItemsCollectionSwitched += feedManager_ItemsCollectionSwitched;
        }

        void feedManager_ItemsCollectionSwitched(object sender, ItemsCollectionSwitchedEventArgs e)
        {
            CollectionViewSource cvsItems = new CollectionViewSource();
            cvsItems.IsLiveSortingRequested = true;
            cvsItems.SortDescriptions.Add(new SortDescription("Published", ListSortDirection.Descending));
            cvsItems.Source = e.Obj;

            BindingOperations.SetBinding(ic_Items, ItemsControl.ItemsSourceProperty, new Binding { Source = cvsItems, Mode = BindingMode.OneWay });
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await this.feedManager.LoadAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
