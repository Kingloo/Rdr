using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RdrLib.Model;

namespace Rdr.Gui
{
	public partial class MainWindow : Window
	{
		private readonly IMainWindowViewModel vm;

		public MainWindow(IMainWindowViewModel viewModel)
		{
			InitializeComponent();

			vm = viewModel;

			DataContext = vm;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			vm.ReloadCommand.Execute();

			vm.StartRefreshTimer();
		}

		private void Window_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				vm.ExitCommand.Execute(this);
			}
		}

		private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Label label = (Label)sender;
			Feed feed = (Feed)label.DataContext;

			vm.ViewFeedItemsCommand.Execute(feed);
		}

		private void Window_Closing(object sender, CancelEventArgs e)
		{
			if (vm.Activity)
			{
				MessageBoxResult result = MessageBox.Show(
					"I am doing something. Do you really want to quit?",
					"Activity!",
					MessageBoxButton.YesNo,
					MessageBoxImage.Exclamation);

				if (result == MessageBoxResult.No)
				{
					e.Cancel = true;
				}
			}
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			vm.StopRefreshTimer();
		}
	}
}
