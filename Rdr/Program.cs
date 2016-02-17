using System;
using System.Globalization;

[assembly:CLSCompliant(true)]
namespace Rdr
{
    public static class Program
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
                string errorMessage = string.Format(CultureInfo.CurrentCulture, "exited with code {0}", exitCode);

                Utils.LogMessage(errorMessage);
            }

            return exitCode;
        }
    }
}
