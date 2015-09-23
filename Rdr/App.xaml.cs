using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace Rdr
{
    public partial class App : Application
    {
        public App()
        {
            ServicePointManager.DefaultConnectionLimit = 9;
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Utils.LogException(e.Exception);
        }
    }
}
