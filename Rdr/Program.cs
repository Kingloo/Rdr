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
            FileInfo feedsFile = GetFeedsFile();

            if (feedsFile == null)
            {
                return -1;
            }

            IRepo feedsRepo = new TxtRepo(feedsFile);

            App app = new App(feedsRepo);

            int exitCode = app.Run();

            if (exitCode != 0)
            {
                string errorMessage = Invariant($"exited with code {exitCode}");

                Log.LogMessage(errorMessage);
            }

            return exitCode;
        }

        private static FileInfo GetFeedsFile()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (String.IsNullOrWhiteSpace(dir))
            {
                return null;
            }

#if DEBUG
            string filename = "RdrFeeds-test.txt";
#else
            string filename = "RdrFeeds.txt";
#endif

            string fullPath = Path.Combine(dir, filename);
            
            if (!File.Exists(fullPath))
            {
                using (StreamWriter sw = File.CreateText(fullPath)) { }
            }
            
            return new FileInfo(fullPath);
        }
    }
}
