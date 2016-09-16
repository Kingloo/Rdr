using System.Net;
using System.Windows;
using System.Windows.Threading;

namespace Rdr
{
    public partial class App : Application
    {
        public IRepo Repo { get; private set; }

        public App(IRepo repo)
        {
            Repo = repo;

            ServicePointManager.DefaultConnectionLimit = 10;
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Utils.LogException(e.Exception);
        }
    }
}
