using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using RdrLib.Model;

namespace Rdr.Gui
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel vm;

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

            vm = viewModel;

            DataContext = vm;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            vm.ReloadCommand.Execute(null);

            vm.StartTimer();
        }

        private void SeeUnread(object sender, RoutedEventArgs e)
        {
            SetItemsBinding(vm.UnreadItems);
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // hard-casts mean that if this method is ever passed anything else it will throw
            // and InvalidCastException
            // this is desired behaviour - calling this method with anything else is a mistake that must be corrected

            Label label = (Label)sender;
            Feed feed = (Feed)label.DataContext;

            SetItemsBinding(feed.Items);
        }

        private void SetItemsBinding(IReadOnlyCollection<Item> source)
        {
            var cvs = (CollectionViewSource)Resources["sortedItems"];

            cvs.Source = source;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (vm.Activity || vm.HasActiveDownload)
            {
                MessageBoxResult result = MessageBox.Show("I am doing something. Do you really want to quit?", "Activity!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
