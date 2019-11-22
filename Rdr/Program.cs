using System;
using System.Collections.Generic;
using System.Text;

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
                // log
            }

            return 0;
        }
    }
}
