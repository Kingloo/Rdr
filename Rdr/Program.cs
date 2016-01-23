using System;

namespace Rdr
{
    public class Program
    {
        [STAThread]
        public static int Main()
        {
            string feedsFilePath = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"\RdrFeeds.txt");

            IRepo feedsRepo = new TxtRepo(feedsFilePath);

            App app = new App(feedsRepo);
            app.InitializeComponent();

            int exitCode = app.Run();

            if (exitCode != 0)
            {
                string errorMessage = $"exited with code {exitCode}";

                Utils.LogMessage(errorMessage);
            }

            return exitCode;
        }
    }
}
