using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            if (sender is Label lbl)
            {
                if (lbl.DataContext is Feed feed)
                {
                    SetItemsBinding(feed.Items);
                }
            }
        }

        private void SetItemsBinding(IReadOnlyCollection<Item> source)
        {
            var cvs = (CollectionViewSource)Resources["sortedItems"];

            cvs.Source = source;
        }
    }
}
