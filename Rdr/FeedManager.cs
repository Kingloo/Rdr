using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Rdr.Extensions;

namespace Rdr
{
    internal class FeedManager : RdrBase
    {
        #region Events
        public event EventHandler FeedChanged;
        private void OnFeedChanged(RdrFeed feed)
        {
            EventHandler eh = FeedChanged;

            if (eh != null)
            {
                eh(feed, new EventArgs());
            }
        }
        #endregion
        
        #region Commands
        private DelegateCommandAsync _refreshAllFeedsCommandAsync = null;
        public DelegateCommandAsync RefreshAllFeedsCommandAsync
        {
            get
            {
                if (_refreshAllFeedsCommandAsync == null)
                {
                    _refreshAllFeedsCommandAsync = new DelegateCommandAsync(RefreshAllFeedsAsync, canExecuteAsync);
                }

                return _refreshAllFeedsCommandAsync;
            }
        }

        public async Task RefreshAllFeedsAsync()
        {
            if (Feeds.Count > 0)
            {
                IEnumerable<Task> refreshTasks = from each in this.Feeds
                                                 where !each.Updating
                                                 select RefreshFeedAsync(each);

                await Task.WhenAll(refreshTasks).ConfigureAwait(false);
            }   
        }

        // we do not cache so that we can refresh a second feed manually before the first one has finished
        public DelegateCommandAsync<RdrFeed> RefreshFeedCommandAsync
        {
            get
            {
                return new DelegateCommandAsync<RdrFeed>(RefreshFeedAsync, (_) => { return true; });
            }
        }

        private async Task RefreshFeedAsync(RdrFeed feed)
        {
            if (feed.Name.Equals("Unread")) return;

            feed.Updating = true;
            Activity = activeTasks.Count() > 0;

            string websiteAsString = await GetFeed(feed.XmlUrl);

            if (String.IsNullOrWhiteSpace(websiteAsString))
            {
                feed.Updating = false;
                Activity = activeTasks.Count() > 0;

                return;
            }

            websiteAsString = websiteAsString.Replace((char)(0x1F), (char)(0x20)); // removing this breaks something

            XDocument x = ParseWebsiteStringIntoXDocument(websiteAsString, feed.XmlUrl);

            if (x == null)
            {
                feed.Updating = false;
                Activity = activeTasks.Count() > 0;

                return;
            }

            feed.Load(x);

            AddUnreadItemsToUnreadCollector(feed.Items);

            feed.Updating = false;
            Activity = activeTasks.Count() > 0;

            _feeds.AlternativeSort(unreadCollector, null);
        }

        private static async Task<string> GetFeed(Uri uri)
        {
            HttpWebRequest req = BuildHttpWebRequest(uri);

            return await Utils.DownloadWebsiteAsStringAsync(req).ConfigureAwait(false);
        }

        private static XDocument ParseWebsiteStringIntoXDocument(string websiteAsString, Uri feedUri)
        {
            XDocument x = null;

            try
            {
                x = XDocument.Parse(websiteAsString);
            }
            catch (XmlException e)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, "{0}: website string did not parse into XDocument", feedUri.AbsoluteUri);

                Utils.LogException(e, errorMessage);

                x = null;
            }

            return x;
        }

        private void AddUnreadItemsToUnreadCollector(IEnumerable<RdrFeedItem> feedItems)
        {
            IEnumerable<RdrFeedItem> unreadItems = from each in feedItems
                                                   where each.Unread
                                                   select each;

            unreadCollector.Items.AddMissing<RdrFeedItem>(unreadItems);
        }

        private DelegateCommand _markAllItemsAsReadCommand = null;
        public DelegateCommand MarkAllItemsAsReadCommand
        {
            get
            {
                if (_markAllItemsAsReadCommand == null)
                {
                    _markAllItemsAsReadCommand = new DelegateCommand(MarkAllItemsAsRead, canExecute);
                }

                return _markAllItemsAsReadCommand;
            }
        }

        private void MarkAllItemsAsRead()
        {
            unreadCollector.Items.Clear();

            foreach (RdrFeed feed in Feeds)
            {
                foreach (RdrFeedItem item in feed.Items)
                {
                    MarkItemAsRead(item);
                }
            }
        }

        private void MarkItemAsRead(RdrFeedItem item)
        {
            item.MarkAsRead();

            if (unreadCollector.Items.Contains<RdrFeedItem>(item))
            {
                unreadCollector.Items.Remove(item);
            }
        }

        private DelegateCommand<RdrFeed> _goToFeedCommand = null;
        public DelegateCommand<RdrFeed> GoToFeedCommand
        {
            get
            {
                if (_goToFeedCommand == null)
                {
                    _goToFeedCommand = new DelegateCommand<RdrFeed>(GoToFeed, canExecute);
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
                if (_goToItemCommand == null)
                {
                    _goToItemCommand = new DelegateCommand<RdrFeedItem>(GoToItem, canExecute);
                }

                return _goToItemCommand;
            }
        }

        private void GoToItem(RdrFeedItem feedItem)
        {
            Utils.OpenUriInBrowser(feedItem.Link);

            MarkItemAsRead(feedItem);
        }

        private DelegateCommand<RdrFeed> _moveItemsToViewCommand = null;
        public DelegateCommand<RdrFeed> MoveItemsToViewCommand
        {
            get
            {
                if (_moveItemsToViewCommand == null)
                {
                    _moveItemsToViewCommand = new DelegateCommand<RdrFeed>(MoveItemsToView, canExecute);
                }

                return _moveItemsToViewCommand;
            }
        }

        public void MoveItemsToView(RdrFeed feed)
        {
            OnFeedChanged(feed);
        }

        private DelegateCommand _openFeedsFileCommand = null;
        public DelegateCommand OpenFeedsFileCommand
        {
            get
            {
                if (_openFeedsFileCommand == null)
                {
                    _openFeedsFileCommand = new DelegateCommand(OpenFeedsFile, canExecute);
                }

                return _openFeedsFileCommand;
            }
        }

        private void OpenFeedsFile()
        {
            try
            {
                Process.Start("notepad.exe", (App.Current as App).Repo.FilePath);
            }
            catch (FileNotFoundException)
            {
                Process.Start((App.Current as App).Repo.FilePath);
            }
        }

        private DelegateCommandAsync _loadFeedsCommandAsync = null;
        public DelegateCommandAsync LoadFeedsCommandAsync
        {
            get
            {
                if (_loadFeedsCommandAsync == null)
                {
                    _loadFeedsCommandAsync = new DelegateCommandAsync(LoadFeedsAsync, canExecuteAsync);
                }

                return _loadFeedsCommandAsync;
            }
        }

        private async Task LoadFeedsAsync()
        {
            IEnumerable<string> feedUris = await ((App)(App.Current)).Repo.LoadAsync();

            List<RdrFeed> feeds = new List<RdrFeed>();

            foreach (string each in feedUris)
            {
                RdrFeed feed = new RdrFeed(new Uri(each));

                feeds.Add(feed);
            }
            
            _feeds.AddMissing(feeds);

            List<RdrFeed> toBeRemoved = (from each in Feeds
                                         where (feeds.Contains<RdrFeed>(each) == false) && (each.Name.Equals("Unread") == false)
                                         select each)
                                         .ToList();

            _feeds.RemoveList(toBeRemoved);
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
            if (arg.DownloadLink == null)
            {
                Utils.LogMessage("enclosure download link is null");

                return;
            }

            arg.Downloading = true;
            
            HttpWebRequest req = HttpWebRequest.CreateHttp(arg.DownloadLink);

            using (HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsyncExt().ConfigureAwait(false))
            {
                if (resp != null)
                {
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        using (Stream input = resp.GetResponseStream())
                        {
                            string fullLocalFilePath = DetermineFullLocalFilePath(arg.DownloadLink);

                            int bufferSize = 6144; // 4096 + 2048

                            using (FileStream fsAsync = new FileStream(fullLocalFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true))
                            {
                                int bytesRead = 0;
                                decimal totalBytesRead = 0;
                                decimal totalBytes = Convert.ToDecimal(resp.ContentLength);
                                int percentDone = 0;
                                byte[] buffer = new byte[bufferSize];

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
                    else // if resp.StatusCode != HttpStatusCode.OK
                    {
                        string errorMessage = string.Format("A request to {0} failed with code {1}", arg.DownloadLink, resp.StatusCode);

                        await Utils.LogMessageAsync(errorMessage).ConfigureAwait(false);
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
                if (_exitCommand == null)
                {
                    _exitCommand = new DelegateCommand(Exit, canExecute);
                }

                return _exitCommand;
            }
        }

        private void Exit()
        {
            // App.xaml:ShutdownMode->OnMainWindowClose
            // see MainWindow.xaml.cs for Closing event handler

            System.Windows.Application.Current.MainWindow.Close();
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
        private const string appName = "Rdr";
        private readonly string downloadDirectory = string.Concat(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"\share\");
        private IEnumerable<RdrFeed> activeTasks;
        private RdrFeed unreadCollector = new RdrFeed("Unread");
        private DispatcherTimer updateAllTimer = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 25, 0)
        };
        #endregion

        #region Properties
        public string WindowTitle
        {
            get
            {
                if (this.Activity)
                {
                    return string.Format(CultureInfo.CurrentCulture, "{0} - updating", appName);
                }
                else
                {
                    return appName;
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
                if (this._activity != value)
                {
                    this._activity = value;

                    OnNotifyPropertyChanged();
                    OnNotifyPropertyChanged("WindowTitle");

                    this.RaiseAllAsyncCanExecuteChangedCommands();
                }
            }
        }

        private void RaiseAllAsyncCanExecuteChangedCommands()
        {
            RefreshAllFeedsCommandAsync.RaiseCanExecuteChanged();
            RefreshFeedCommandAsync.RaiseCanExecuteChanged();
            LoadFeedsCommandAsync.RaiseCanExecuteChanged();
        }

        private readonly ObservableCollection<RdrFeed> _feeds = new ObservableCollection<RdrFeed>();
        //public ObservableCollection<RdrFeed> Feeds { get { return _feeds; } }
        public IReadOnlyCollection<RdrFeed> Feeds { get { return _feeds; } }

        private readonly ObservableCollection<RdrFeedItem> _items = new ObservableCollection<RdrFeedItem>();
        //public ObservableCollection<RdrFeedItem> Items { get { return _items; } }
        public IReadOnlyCollection<RdrFeedItem> Items { get { return _items; } }
        #endregion

        public FeedManager()
        {
            Application.Current.MainWindow.Loaded += async (sender, e) => await LoadFeedsAsync();

            activeTasks = from each in Feeds
                          where each.Updating
                          select each;

            _feeds.Add(unreadCollector);
            _feeds.CollectionChanged += Feeds_CollectionChanged;

            updateAllTimer.Tick += async (sender, e) => await RefreshAllFeedsAsync();
            updateAllTimer.Start();
        }

        private async void Feeds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                IEnumerable<Task> refreshTasks = from each in e.NewItems.Cast<RdrFeed>()
                                                 where each.Updating == false
                                                 select RefreshFeedAsync(each);

                await Task.WhenAll(refreshTasks).ConfigureAwait(false);
            }

            _feeds.AlternativeSort<RdrFeed>(unreadCollector, null);
        }
        
        private static HttpWebRequest BuildHttpWebRequest(Uri xmlUrl)
        {
            HttpWebRequest req = WebRequest.CreateHttp(xmlUrl);

            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.CachePolicy = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            req.Host = xmlUrl.DnsSafeHost;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version11;
            req.Referer = string.Format(CultureInfo.CurrentCulture, "{0}{1}/", xmlUrl.GetLeftPart(UriPartial.Scheme), xmlUrl.DnsSafeHost);
            req.Timeout = 2500;
            req.UserAgent = @"Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            req.Headers.Add("DNT", "1");
            req.Headers.Add("Accept-Encoding", "gzip, deflate"); // to match the choices made for AutomaticDecompression

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
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Feeds: {0}", Feeds.Count));
            sb.AppendLine(string.Format(CultureInfo.CurrentCulture, "Items: {0}", Items.Count));

            return sb.ToString();
        }
    }
}
