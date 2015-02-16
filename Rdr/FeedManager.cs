using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Rdr
{
    class FeedManager : RdrBase
    {
        #region Commands
        private DelegateCommandAsync _refreshAllFeedsCommandAsync = null;
        public DelegateCommandAsync RefreshAllFeedsCommandAsync
        {
            get
            {
                if (this._refreshAllFeedsCommandAsync == null)
                {
                    this._refreshAllFeedsCommandAsync = new DelegateCommandAsync(RefreshAllFeedsAsync, canExecuteAsync);
                }

                return this._refreshAllFeedsCommandAsync;
            }
        }

        public async Task RefreshAllFeedsAsync()
        {
            if (this.Feeds.Count > 0)
            {
                IEnumerable<Task> refreshTasks = from each in this.Feeds
                                                 where each.Updating == false
                                                 select RefreshFeedAsync(each);

                await Task.WhenAll(refreshTasks);
            }   
        }

        public DelegateCommandAsync<RdrFeed> RefreshFeedCommandAsync
        {
            get
            {
                return new DelegateCommandAsync<RdrFeed>(RefreshFeedAsync, (_) => { return true; });
            }
        }

        private async Task RefreshFeedAsync(RdrFeed feed)
        {
            feed.Updating = true;
            this.Activity = (activeTasks.Count<RdrFeed>() > 0);

            OnNotifyPropertyChanged("WindowTitle");

            HttpWebRequest req = BuildHttpWebRequest(feed.XmlUrl);
            string websiteAsString = await Utils.DownloadWebsiteAsStringAsync(req, 2);
            //string websiteAsString = await Utils.DownloadWebsiteAsStringAsyncComplicated(req);
            //string websiteAsString = await Utils.HttpGetStringAsync(feed.XmlUrl);

            if (String.IsNullOrEmpty(websiteAsString) == false)
            {
                websiteAsString = websiteAsString.Replace((char)(0x1F), (char)(0x20));
                
                XDocument x = null;

                try
                {
                    x = XDocument.Parse(websiteAsString);
                }
                catch (XmlException e)
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine(feed.XmlUrl.AbsoluteUri);
                    sb.AppendLine(e.Message);
                    sb.AppendLine(e.StackTrace);

                    Utils.LogMessage(sb.ToString());

                    x = null;
                }

                if (x != null)
                {
                    feed.Load(x);

                    MoveUnreadItemsToView(feed);
                }
            }

            feed.Updating = false;
            this.Activity = (activeTasks.Count<RdrFeed>() > 0);

            OnNotifyPropertyChanged("WindowTitle");
        }

        private DelegateCommand _markAllItemsAsReadCommand = null;
        public DelegateCommand MarkAllItemsAsReadCommand
        {
            get
            {
                if (this._markAllItemsAsReadCommand == null)
                {
                    this._markAllItemsAsReadCommand = new DelegateCommand(new Action(MarkAllItemsAsRead), canExecute);
                }

                return _markAllItemsAsReadCommand;
            }
        }

        private void MarkAllItemsAsRead()
        {
            foreach (RdrFeed each in this.Feeds)
            {
                each.MarkAllItemsAsRead();
            }

            MoveAllUnreadItemsToView();
        }

        private DelegateCommand<RdrFeed> _goToFeedCommand = null;
        public DelegateCommand<RdrFeed> GoToFeedCommand
        {
            get
            {
                if (this._goToFeedCommand == null)
                {
                    this._goToFeedCommand = new DelegateCommand<RdrFeed>(new Action<RdrFeed>(GoToFeed), canExecute);
                }

                return _goToFeedCommand;
            }
        }

        private void GoToFeed(RdrFeed feed)
        {
            Utils.OpenUriInBrowser(feed.XmlUrl);
        }

        private DelegateCommand<RdrFeedItem> _goToItemCommand = null;
        public DelegateCommand<RdrFeedItem> GoToItemCommand
        {
            get
            {
                if (this._goToItemCommand == null)
                {
                    this._goToItemCommand = new DelegateCommand<RdrFeedItem>(new Action<RdrFeedItem>(GoToItem), canExecute);
                }

                return _goToItemCommand;
            }
        }

        private void GoToItem(RdrFeedItem feedItem)
        {
            Utils.OpenUriInBrowser(feedItem.Link);

            feedItem.Unread = false;
        }

        private DelegateCommand<RdrFeed> _moveItemsToViewCommand = null;
        public DelegateCommand<RdrFeed> MoveItemsToViewCommand
        {
            get
            {
                if (this._moveItemsToViewCommand == null)
                {
                    this._moveItemsToViewCommand = new DelegateCommand<RdrFeed>(new Action<RdrFeed>(MoveItemsToView), canExecute);
                }

                return _moveItemsToViewCommand;
            }
        }

        private void MoveItemsToView(RdrFeed feed)
        {
            lock (this.Items)
            {
                this.Items.Clear();

                IEnumerable<RdrFeedItem> feedItems = from each in feed.Items
                                                     select each;

                this.Items.AddList<RdrFeedItem>(feedItems);
            }
        }

        private DelegateCommand _moveUnreadItemsToViewCommand = null;
        public DelegateCommand MoveUnreadItemsToViewCommand
        {
            get
            {
                if (this._moveUnreadItemsToViewCommand == null)
                {
                    this._moveUnreadItemsToViewCommand = new DelegateCommand(MoveAllUnreadItemsToView, canExecute);
                }

                return _moveUnreadItemsToViewCommand;
            }
        }

        private void MoveAllUnreadItemsToView()
        {
            lock (this.Items)
            {
                this.Items.Clear();

                foreach (RdrFeed each in this.Feeds)
                {
                    MoveUnreadItemsToView(each);
                }
            }
        }

        private void MoveUnreadItemsToView(RdrFeed feed)
        {
            IEnumerable<RdrFeedItem> unreadItems = from each in feed.Items
                                                   where each.Unread
                                                   select each;

            this.Items.AddMissing<RdrFeedItem>(unreadItems);
        }

        private DelegateCommand _openFeedsFileCommand = null;
        public DelegateCommand OpenFeedsFileCommand
        {
            get
            {
                if (this._openFeedsFileCommand == null)
                {
                    this._openFeedsFileCommand = new DelegateCommand(OpenFeedsFile, canExecute);
                }

                return this._openFeedsFileCommand;
            }
        }

        private void OpenFeedsFile()
        {
            try
            {
                Process.Start("notepad.exe", Program.FeedsFile);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(string.Format("The feeds file:{0}{1}{0}was not found.", Environment.NewLine, Program.FeedsFile), "File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private DelegateCommandAsync _reloadFeedsCommandAsync = null;
        public DelegateCommandAsync ReloadFeedsCommandAsync
        {
            get
            {
                if (this._reloadFeedsCommandAsync == null)
                {
                    this._reloadFeedsCommandAsync = new DelegateCommandAsync(ReloadFeedsAsync, canExecuteAsync);
                }

                return this._reloadFeedsCommandAsync;
            }
        }

        private async Task ReloadFeedsAsync()
        {
            IEnumerable<RdrFeed> loadedFeeds = await Program.LoadFeedsFromFile();

            this.Feeds.AddMissing<RdrFeed>(loadedFeeds);

            List<RdrFeed> toBeRemoved = new List<RdrFeed>();

            foreach (RdrFeed each in this.Feeds)
            {
                if (loadedFeeds.Contains<RdrFeed>(each) == false)
                {
                    toBeRemoved.Add(each);
                }
            }

            this.Feeds.RemoveList<RdrFeed>(toBeRemoved);
        }
        
        // we deliberately don't cache this download command so that each enclosure gets its own
        // otherwise starting a single download would prevent starting any other
        public DelegateCommandAsync<RdrEnclosure> DownloadEnclosureCommandAsync
        {
            get
            {
                return new DelegateCommandAsync<RdrEnclosure>(DownloadEnclosureAsync, (enclosure) => { return !enclosure.Downloading; });
            }
        }

        private async Task DownloadEnclosureAsync(RdrEnclosure arg)
        {
            arg.Downloading = true;
            HttpWebRequest req = HttpWebRequest.CreateHttp(arg.DownloadLink);

            using (HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsyncExt(false).ConfigureAwait(false))
            {
                if (resp != null)
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream input = resp.GetResponseStream())
                        {
                            string fullLocalFilePath = DetermineFullLocalFilePath(arg.DownloadLink);

                            using (FileStream fsAsync = new FileStream(fullLocalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024, true))
                            {
                                int bytesRead = 0;
                                decimal totalBytesRead = 0;
                                decimal totalBytes = Convert.ToDecimal(resp.ContentLength);
                                int percentDone = 0;
                                byte[] buffer = new byte[1024];

                                while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                                {
                                    totalBytesRead += bytesRead;
                                    percentDone = Convert.ToInt32(((totalBytesRead / totalBytes) * 100));

                                    arg.ButtonText = string.Format("{0} %", percentDone.ToString());

                                    await fsAsync.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                                }

                                arg.ButtonText = "Downloaded";
                            }
                        }
                    }
                }
            }

            arg.Downloading = false;
        }

        private string DetermineFullLocalFilePath(Uri uri)
        {
            string fileName = uri.Segments.Last<string>();

            return string.Concat(downloadDirectory, fileName);
        }

        private DelegateCommand _exitCommand = null;
        public DelegateCommand ExitCommand
        {
            get
            {
                if (this._exitCommand == null)
                {
                    this._exitCommand = new DelegateCommand(Exit, canExecute);
                }

                return this._exitCommand;
            }
        }

        private void Exit()
        {
            // App.xaml:ShutdownMode->OnMainWindowClose
            // see MainWindow.xaml.cs for Closing event handler

            Application.Current.MainWindow.Close();
        }

        private bool canExecute(object _)
        {
            return true;
        }

        private bool canExecuteAsync(object _)
        {
            return !this.Activity;
        }
        #endregion

        #region Fields
        //private const string appName = "Rdr";
        private readonly string downloadDirectory = string.Format(@"C:\Users\{0}\Documents\share\", Environment.UserName);
        private readonly DispatcherTimer updateAllTimer = null;
        private IEnumerable<RdrFeed> activeTasks;
        #endregion

        #region Properties
        public string WindowTitle
        {
            get
            {
                if (this.Activity)
                {
                    return string.Format("{0} - updating", Program.AppName);
                }
                else
                {
                    return Program.AppName;
                }
            }
        }

        private bool _activity = false;
        public bool Activity
        {
            get
            {
                return this._activity;
            }
            set
            {
                this._activity = value;
                OnNotifyPropertyChanged();

                this.RaiseAllAsyncCanExecuteChangedCommands();
            }
        }

        private void RaiseAllAsyncCanExecuteChangedCommands()
        {
            this.RefreshAllFeedsCommandAsync.RaiseCanExecuteChanged();
            this.RefreshFeedCommandAsync.RaiseCanExecuteChanged();
            this.ReloadFeedsCommandAsync.RaiseCanExecuteChanged();
        }

        private ObservableCollection<RdrFeed> _feeds = new ObservableCollection<RdrFeed>();
        public ObservableCollection<RdrFeed> Feeds { get { return this._feeds; } }

        private ObservableCollection<RdrFeedItem> _items = new ObservableCollection<RdrFeedItem>();
        public ObservableCollection<RdrFeedItem> Items { get { return this._items; } }
        #endregion

        public FeedManager()
        {
            this.activeTasks = from each in this.Feeds
                               where each.Updating
                               select each;

            this.Feeds.CollectionChanged += Feeds_CollectionChanged;

            this.updateAllTimer = new DispatcherTimer
            {
#if DEBUG
                Interval = new TimeSpan(0, 2, 0)
#else
                Interval = new TimeSpan(0, 20, 0)
#endif
            };

            this.updateAllTimer.Tick += updateAllTimer_Tick;
            this.updateAllTimer.IsEnabled = true;

            this.Feeds.AddList<RdrFeed>(Program.Feeds);
        }

        private async void Feeds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (RdrFeed each in e.NewItems)
                {
                    await RefreshFeedAsync(each).ConfigureAwait(false);
                }
            }
        }

        private async void updateAllTimer_Tick(object sender, EventArgs e)
        {
            await RefreshAllFeedsAsync();
        }

        private HttpWebRequest BuildHttpWebRequest(Uri xmlUrl)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(xmlUrl);

            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.BypassCache);
            req.Host = xmlUrl.DnsSafeHost;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version11;
            req.Referer = string.Format("{0}://{1}/", xmlUrl.GetLeftPart(UriPartial.Scheme), xmlUrl.DnsSafeHost);
            req.Timeout = 4500;
            req.UserAgent = @"Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            req.Headers.Add("DNT", "1");
            req.Headers.Add("Accept-Encoding", "gzip, deflate");

            if (xmlUrl.Scheme.Equals("https"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }

            return req;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("Feeds file: {0}", Program.FeedsFile));
            sb.AppendLine(string.Format("Feeds: {0}", this.Feeds.Count));
            sb.AppendLine(string.Format("Items: {0}", this.Items.Count));

            return sb.ToString();
        }
    }
}
