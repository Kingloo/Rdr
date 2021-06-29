using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Rdr.Common;

namespace Rdr.Gui
{
	public partial class App : Application
	{
		private readonly string defaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

#if DEBUG
		private const string defaultFileName = "RdrFeeds-test.txt";
#else
        private const string defaultFileName = "RdrFeeds.txt";
#endif

		public App()
		{
			InitializeComponent();
		}

		private void Application_Startup(object sender, StartupEventArgs e)
		{
			string path = Path.Combine(defaultDirectory, defaultFileName);

			MainWindowViewModel viewModel = new MainWindowViewModel(path);

			MainWindow = new MainWindow(viewModel);

			MainWindow.Show();
		}

		private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (e.Exception is Exception ex)
			{
				LogStatic.Exception(ex, includeStackTrace: true);
			}
			else
			{
				string message = "a DispatcherUnhandledException happened, but was empty";

				LogStatic.Message(message);
			}
		}
	}
}
