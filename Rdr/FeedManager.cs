using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Linq;
using Rdr.Fidr;

namespace Rdr
{
    class FeedManager : RdrBase
    {
        #region Commands
        private DelegateCommandAsync<object> _refreshAllFeedsCommandAsync = null;
        public DelegateCommandAsync<object> RefreshAllFeedsCommandAsync
        {
            get
            {
                if (this._refreshAllFeedsCommandAsync == null)
                {
                    this._refreshAllFeedsCommandAsync = new DelegateCommandAsync<object>(new Func<object, Task>(RefreshAllFeedsAsync), canExecuteAsync);
                }

                return this._refreshAllFeedsCommandAsync;
            }
        }

        public async Task RefreshAllFeedsAsync(object parameter)
        {
            this.Activity = true;

            if (this.Feeds.Count > 0)
            {
                IEnumerable<Task> refreshTasks = from each in this.Feeds
                                                 select RefreshFeedAsync(each);

                await Task.WhenAll(refreshTasks);
            }

            this.Activity = false;
        }

        private DelegateCommandAsync<Feed> _refreshFeedCommandAsync = null;
        public DelegateCommandAsync<Feed> RefreshFeedCommandAsync
        {
            get
            {
                if (this._refreshFeedCommandAsync == null)
                {
                    this._refreshFeedCommandAsync = new DelegateCommandAsync<Feed>(new Func<Feed, Task>(RefreshFeedAsync), canExecuteAsync);
                }

                return this._refreshFeedCommandAsync;
            }
        }

        private async Task RefreshFeedAsync(Feed feed)
        {
            feed.Updating = true;

            HttpWebRequest req = BuildWebRequest(feed.XmlUrl);
            string websiteAsString = await Misc.DownloadWebsiteAsString(req);

            if (String.IsNullOrEmpty(websiteAsString) == false)
            {
                feed.Load(websiteAsString);

                MoveUnreadItemsToView(feed);
            }

            feed.Updating = false;
        }

        private DelegateCommand<object> _markAllItemsAsReadCommand = null;
        public DelegateCommand<object> MarkAllItemsAsReadCommand
        {
            get
            {
                if (this._markAllItemsAsReadCommand == null)
                {
                    this._markAllItemsAsReadCommand = new DelegateCommand<object>(new Action<object>(MarkAllItemsAsRead), canExecute);
                }

                return _markAllItemsAsReadCommand;
            }
        }

        private void MarkAllItemsAsRead(object _)
        {
            foreach (Feed each in this.Feeds)
            {
                if (each != null)
                {
                    each.MarkAllItemsAsRead();
                }
            }

            MoveAllUnreadItemsToView(null);
        }

        private DelegateCommand<Feed> _goToFeedCommand = null;
        public DelegateCommand<Feed> GoToFeedCommand
        {
            get
            {
                if (this._goToFeedCommand == null)
                {
                    this._goToFeedCommand = new DelegateCommand<Feed>(new Action<Feed>(GoToFeed), canExecute);
                }

                return _goToFeedCommand;
            }
        }

        private void GoToFeed(Feed feed)
        {
            Misc.OpenUrlInBrowser(feed.XmlUrl);
        }

        private DelegateCommand<FeedItem> _goToItemCommand = null;
        public DelegateCommand<FeedItem> GoToItemCommand
        {
            get
            {
                if (this._goToItemCommand == null)
                {
                    this._goToItemCommand = new DelegateCommand<FeedItem>(new Action<FeedItem>(GoToItem), canExecute);
                }

                return _goToItemCommand;
            }
        }

        private void GoToItem(FeedItem feedItem)
        {
            Misc.OpenUrlInBrowser(feedItem.Link);

            feedItem.Unread = false;
        }

        private DelegateCommand<Feed> _moveItemsToViewCommand = null;
        public DelegateCommand<Feed> MoveItemsToViewCommand
        {
            get
            {
                if (this._moveItemsToViewCommand == null)
                {
                    this._moveItemsToViewCommand = new DelegateCommand<Feed>(new Action<Feed>(MoveItemsToView), canExecute);
                }

                return _moveItemsToViewCommand;
            }
        }

        private void MoveItemsToView(Feed feed)
        {
            lock (this.Items)
            {
                this.Items.Clear();

                IEnumerable<FeedItem> feedItems = from each in feed.FeedItems
                                                  select each;

                this.Items.AddList<FeedItem>(feedItems);
            }
        }

        private DelegateCommand<object> _moveUnreadItemsToViewCommand = null;
        public DelegateCommand<object> MoveUnreadItemsToViewCommand
        {
            get
            {
                if (this._moveUnreadItemsToViewCommand == null)
                {
                    this._moveUnreadItemsToViewCommand = new DelegateCommand<object>(new Action<object>(MoveAllUnreadItemsToView), canExecute);
                }

                return _moveUnreadItemsToViewCommand;
            }
        }

        private void MoveUnreadItemsToView(Feed feed)
        {
            IEnumerable<FeedItem> unreadItems = from each in feed.FeedItems
                                                where each.Unread
                                                select each;

            this.Items.AddMissingItems<FeedItem>(unreadItems);
        }
        #endregion

        #region Fields
        private readonly MainWindow mainWindow = null;
        private readonly string feedsFile = string.Format(@"C:\Users\{0}\Documents\rssfeeds.xml", Environment.UserName);
        private readonly DispatcherTimer updateAllTimer = new DispatcherTimer();
        #endregion

        #region Properties
        private bool _activity = false;
        private bool Activity
        {
            get
            {
                return this._activity;
            }
            set
            {
                this._activity = value;
                OnNotifyPropertyChanged();
            }
        }

        private ObservableCollection<Feed> _feeds = new ObservableCollection<Feed>();
        public ObservableCollection<Feed> Feeds { get { return this._feeds; } }

        private ObservableCollection<FeedItem> _items = new ObservableCollection<FeedItem>();
        public ObservableCollection<FeedItem> Items { get { return this._items; } }
        #endregion

        public FeedManager(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            mainWindow.Loaded += mainWindow_Loaded;
            mainWindow.Closing += mainWindow_Closing;

            this.updateAllTimer.Interval = new TimeSpan(0, 20, 0);
            this.updateAllTimer.Tick += updateTimer_Tick;
            this.updateAllTimer.IsEnabled = true;
        }

        private void updateTimer_Tick(object sender, EventArgs e)
        {
            this.RefreshAllFeedsCommandAsync.Execute(null);
        }

        private async void mainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (FeedsFileExists())
            {
                await LoadAsync();
            }
            else
            {
                await WriteBasicFeedsFileAsync();
            }
        }

        private bool FeedsFileExists()
        {
            if (String.IsNullOrEmpty(this.feedsFile))
            {
                return false;
            }
            else
            {
                if (File.Exists(this.feedsFile))
                {
                    return true;
                }
            }

            return false;
        }

        public async Task LoadAsync()
        {
            this.Activity = true;
            RaiseAllAsyncCanExecuteChangedCommands();

            IEnumerable<Uri> feedUris = await LoadXmlUrlsFromFileAsync();

            if (feedUris.Count<Uri>() > 0)
            {
                IEnumerable<Task> buildFeedTasks = from each in feedUris
                                                   select BuildFeed(each);

                await Task.WhenAll(buildFeedTasks);

                MoveAllUnreadItemsToView(null);
            }

            this.Activity = false;
            RaiseAllAsyncCanExecuteChangedCommands();
        }

        private async Task<IEnumerable<Uri>> LoadXmlUrlsFromFileAsync()
        {
            string feedsFileAsString = string.Empty;

            using (FileStream fsAsync = new FileStream(this.feedsFile, FileMode.Open, FileAccess.Read, FileShare.None, 1024, true))
            {
                using (StreamReader sr = new StreamReader(fsAsync))
                {
                    feedsFileAsString = await sr.ReadToEndAsync().ConfigureAwait(false);
                }
            }

            if (String.IsNullOrEmpty(feedsFileAsString) == false)
            {
                XDocument xDoc = XDocument.Parse(feedsFileAsString);

                if (xDoc != null)
                {
                    IEnumerable<Uri> feeds = from each in xDoc.Root.Elements("feed")
                                             select new Uri(each.Attribute("xmlurl").Value);

                    return feeds;
                }
            }

            return new List<Uri>(0);
        }

        private async Task WriteBasicFeedsFileAsync()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<feeds>");
            sb.AppendLine("</feeds>");

            using (FileStream fsAsync = new FileStream(this.feedsFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                using (StreamWriter sw = new StreamWriter(fsAsync))
                {
                    await sw.WriteAsync(sb.ToString()).ConfigureAwait(false);
                }
            }
        }

        private async Task<bool> BuildFeed(Uri uri)
        {
            HttpWebRequest req = BuildWebRequest(uri);
            string websiteAsString = await Misc.DownloadWebsiteAsString(req);

            HelperMethods.FeedType type = HelperMethods.DetermineFeedType(websiteAsString);
            Feed feed = null;

            switch (type)
            {
                case HelperMethods.FeedType.Atom:
                    if (AtomFeed.TryCreate(websiteAsString, uri, out feed) == false)
                    {
                        return false;
                    }
                    break;
                case HelperMethods.FeedType.RSS:
                    if (RSSFeed.TryCreate(websiteAsString, uri, out feed) == false)
                    {
                        return false;
                    }
                    break;
                case HelperMethods.FeedType.None:
                    return false;
                default:
                    return false;
            }

            if (feed != null)
            {
                this.Feeds.Add(feed);

                return true;
            }

            return false;
        }

        private HttpWebRequest BuildWebRequest(Uri xmlUrl)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(xmlUrl);

            req.AllowAutoRedirect = true;
            req.Host = xmlUrl.DnsSafeHost;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version11;
            req.Referer = xmlUrl.DnsSafeHost;
            req.ServicePoint.ConnectionLimit = 3;
            req.Timeout = 2000;
            req.UserAgent = @"Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            return req;
        }

        private void MoveAllUnreadItemsToView(object _)
        {
            lock (this.Items)
            {
                this.Items.Clear();

                List<FeedItem> toAdd = new List<FeedItem>();

                foreach (Feed each in this.Feeds)
                {
                    if (each != null)
                    {
                        if (each.FeedItems != null)
                        {
                            toAdd.AddList<FeedItem>(from eeach in each.FeedItems
                                                    where eeach.Unread
                                                    select eeach);
                        }
                    }
                }

                this.Items.AddList<FeedItem>(toAdd);
            }
        }

        private bool canExecuteAsync(object _)
        {
            if (this.Activity)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool canExecute(object _)
        {
            return true;
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            MessageBoxResult mbr = MessageBox.Show("Do you really want to Quit?", "Quit", MessageBoxButton.YesNo, MessageBoxImage.Question);

            switch (mbr)
            {
                case MessageBoxResult.Yes:
                    e.Cancel = false;
                    break;
                case MessageBoxResult.No:
                    e.Cancel = true;
                    break;
                default:
                    e.Cancel = true;
                    break;
            }
        }

        private void RaiseAllAsyncCanExecuteChangedCommands()
        {
            this.RefreshAllFeedsCommandAsync.RaiseCanExecuteChanged();
            this.RefreshFeedCommandAsync.RaiseCanExecuteChanged();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(this.GetType().ToString());
            sb.AppendLine(string.Format("Feeds file: {0}", this.feedsFile));
            sb.AppendLine(string.Format("Feeds: {0}", this.Feeds.Count));
            sb.AppendLine(string.Format("Items: {0}", this.Items.Count));

            return sb.ToString();
        }
    }
}
