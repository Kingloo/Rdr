using System;
using System.Globalization;
using System.IO;

namespace Rdr
{
    public static class Program
    {
        [STAThread]
        public static int Main()
        {
            string userDocsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            
#if DEBUG
            string feedsFileName = "RdrFeeds-test.txt";
#else
            string feedsFileName = "RdrFeeds.txt";
#endif

            string feedsFilePath = Path.Combine(userDocsDir, feedsFileName);

            IRepo feedsRepo = new TxtRepo(feedsFilePath);

            App app = new App(feedsRepo);
            app.InitializeComponent();

            int exitCode = app.Run();

            if (exitCode != 0)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, "exited with code {0}", exitCode);

                Utils.LogMessage(errorMessage);
            }

            return exitCode;
        }
    }
}
