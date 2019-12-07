using System;
using System.Globalization;
using Rdr.Common;
using Rdr.Gui;

namespace Rdr
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            App app = new App();

            int exitCode = app.Run();

            if (exitCode != 0)
            {
                string message = String.Format(CultureInfo.CurrentCulture, "Rdr exited with code {0}", exitCode);

                Log.Message(message);
            }

            return 0;
        }
    }
}
