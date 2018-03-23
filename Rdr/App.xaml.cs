using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Threading;
using Rdr.Common;

namespace Rdr
{
    public partial class App : Application
    {
        public App(FileInfo file, DirectoryInfo directory)
        {
            InitializeComponent();

            ServicePointManager.DefaultConnectionLimit = 10;

            MainWindow = new MainWindow
            {
                DataContext = new FeedManager(file, directory)
            };

            MainWindow.Show();
        }
        
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.LogException(e.Exception, includeStackTrace: true);
        }
    }
}
