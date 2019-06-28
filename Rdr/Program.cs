using System;
using System.Globalization;
using System.IO;
using Rdr.Common;

namespace Rdr
{
    public static class Program
    {
        private static readonly string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        private static readonly string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

#if DEBUG
        private static readonly string filename = "RdrFeeds-test.txt";
#else
        private static readonly string filename = "RdrFeeds.txt";
#endif

        [STAThread]
        public static int Main()
        {
            string feedsFilePath = Path.Combine(myDocuments, filename);
            FileInfo feedsFile = new FileInfo(feedsFilePath);

            string downloadDirectoryPath = Path.Combine(userProfile, "share");
            DirectoryInfo downloadDirectory = new DirectoryInfo(downloadDirectoryPath);

            App app = new App(feedsFile, downloadDirectory);

            int exitCode = app.Run();

            if (exitCode != 0)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, "exited with code {0}", exitCode);

                Log.Message(errorMessage);
            }

            return exitCode;
        }
    }
}
