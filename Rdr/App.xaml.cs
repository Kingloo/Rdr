using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace Rdr
{
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Utils.LogException(e.Exception);
        }
    }
}
