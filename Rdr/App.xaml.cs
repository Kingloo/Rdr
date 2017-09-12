using System.Net;
using System.Windows;
using System.Windows.Threading;
using Rdr.Common;

namespace Rdr
{
    public partial class App : Application
    {
        public App(IRepo feedsRepo)
        {
            InitializeComponent();

            ServicePointManager.DefaultConnectionLimit = 10;

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            MainWindow = new MainWindow(new FeedManager(feedsRepo));

            MainWindow.Show();
        }
        
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Log.LogException(e.Exception, includeStackTrace: true);
        }
    }
}
