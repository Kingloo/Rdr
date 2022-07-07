using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RdrLib.Model;

namespace Rdr.Gui
{
	public partial class MainWindow : Window
	{
		private readonly MainWindowViewModel vm;

		public MainWindow(MainWindowViewModel viewModel)
		{
			InitializeComponent();

			vm = viewModel;

			DataContext = vm;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			vm.ReloadCommand.Execute(null);

			vm.StartTimer();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
			}
		}

		private async void SeeUnread(object sender, RoutedEventArgs e)
		{
			await vm.SetSelectedFeedAsync(null).ConfigureAwait(true);
		}

		private async void SeeAll(object sender, RoutedEventArgs e)
		{
			await vm.SeeAll().ConfigureAwait(true);
		}

		private async void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Label label = (Label)sender;
			Feed feed = (Feed)label.DataContext;

			await vm.SetSelectedFeedAsync(feed).ConfigureAwait(true);
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (vm.Activity || vm.HasActiveDownload)
			{
				MessageBoxResult result = MessageBox.Show("I am doing something. Do you really want to quit?", "Activity!", MessageBoxButton.YesNo, MessageBoxImage.Exclamation);

				if (result == MessageBoxResult.No)
				{
					e.Cancel = true;
				}
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			vm.StopTimer();
		}
	}
}
