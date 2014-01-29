using System;
using System.IO;
using System.Windows;

namespace Rdr
{
    public partial class App : Application
    {
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string logfile = string.Format(@"C:\Users\{0}\Documents\logfile.txt", Environment.UserName);

            using (StreamWriter sw = new StreamWriter(logfile, true))
            {
                sw.WriteLine(string.Format("\n{0}: {1}: {2}: {3}\n", DateTime.Now, Application.Current.ToString(), e.Exception.ToString(), e.Exception.Message));
            }

            e.Handled = true;
        }
    }
}
