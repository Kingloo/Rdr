using System;
using System.IO;
using Rdr.Common;
using static System.FormattableString;

namespace Rdr
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            string feedsFilePath = GetFeedsFilePath();

            IRepo feedsRepo = new TxtRepo(feedsFilePath);

            App app = new App(feedsRepo);

            int exitCode = app.Run();

            if (exitCode != 0)
            {
                string errorMessage = Invariant($"exited with code {exitCode}");

                Log.LogMessage(errorMessage);
            }

            return exitCode;
        }

        private static string GetFeedsFilePath()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

#if DEBUG
            string filename = "RdrFeeds-test.txt";
#else
            string filename = "RdrFeeds.txt";
#endif

            return Path.Combine(dir, filename);
        }
    }
}
