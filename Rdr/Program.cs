using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Windows;

namespace Rdr
{
    public class Program
    {
        #region Properties
        private const string _appName = "Rdr";
        public static string AppName { get { return _appName; } }

        private static readonly string _feedsFile = string.Format(@"C:\Users\{0}\Documents\RdrFeeds.txt", Environment.UserName);
        public static string FeedsFile { get { return _feedsFile; } }

        private static readonly List<RdrFeed> _feeds = new List<RdrFeed>();
        internal static List<RdrFeed> Feeds { get { return _feeds; } }
        #endregion

        [STAThread]
        public static int Main(string[] args)
        {
            if (File.Exists(_feedsFile) == false)
            {
                File.Create(_feedsFile);
            }

            IEnumerable<RdrFeed> loadedFeeds = LoadFeedsFromFile().Result;

            if (loadedFeeds == null)
            {
                MessageBox.Show("There was a fatal problem with your feeds file.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);

                return 0;
            }

            Feeds.AddList<RdrFeed>(loadedFeeds);
            
            App app = new App();
            app.InitializeComponent();

            return app.Run();
        }

        internal static async Task<IEnumerable<RdrFeed>> LoadFeedsFromFile()
        {
            List<RdrFeed> toReturn = new List<RdrFeed>();

            FileStream fs = null;

            try
            {
                fs = new FileStream(_feedsFile, FileMode.Open, FileAccess.Read, FileShare.None, 1024, true);
            }
            catch (DirectoryNotFoundException) { return null; }
            catch (FileNotFoundException) { return null; }
            catch (UnauthorizedAccessException) { return null; }
            catch (SecurityException) { return null; }
            catch (IOException) { return null; }

            using (StreamReader sr = new StreamReader(fs))
            {
                string line = string.Empty;

                while ((line = await sr.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.StartsWith("#") == false)
                    {
                        Uri uri = null;

                        if (Uri.TryCreate(line, UriKind.Absolute, out uri))
                        {
                            RdrFeed feed = new RdrFeed(uri);

                            toReturn.Add(feed);
                        }
                    }
                }
            }
			
			if (fsAsync != null)
			{
				fsAsync.Close();
			}

            return toReturn;
        }
    }
}
