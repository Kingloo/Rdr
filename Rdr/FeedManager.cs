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
using System.Xml;
using System.Xml.Linq;

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

        private DelegateCommandAsync<RdrFeed> _refreshFeedCommandAsync = null;
        public DelegateCommandAsync<RdrFeed> RefreshFeedCommandAsync
        {
            get
            {
                if (this._refreshFeedCommandAsync == null)
                {
                    this._refreshFeedCommandAsync = new DelegateCommandAsync<RdrFeed>(new Func<RdrFeed, Task>(RefreshFeedAsync), canExecuteAsync);
                }

                return this._refreshFeedCommandAsync;
            }
        }

        private async Task RefreshFeedAsync(RdrFeed feed)
        {
            feed.Updating = true;

            HttpWebRequest req = BuildHttpWebRequest(feed.XmlUrl);
            string websiteAsString = await Misc.DownloadWebsiteAsString(req);

            websiteAsString = websiteAsString.Replace((char)(0x1F), (char)(0x20));

            if (String.IsNullOrEmpty(websiteAsString) == false)
            {
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

                    Misc.LogMessage(sb.ToString());

                    x = null;
                }

                if (x != null)
                {
                    feed.Load(x);

                    MoveUnreadItemsToView(feed);
                }
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
            foreach (RdrFeed each in this.Feeds)
            {
                if (each != null)
                {
                    each.MarkAllItemsAsRead();
                }
            }

            MoveAllUnreadItemsToView(null);
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
            Misc.OpenUrlInBrowser(feed.XmlUrl);
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
            Misc.OpenUrlInBrowser(feedItem.Link);

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

        private void MoveUnreadItemsToView(RdrFeed feed)
        {
            IEnumerable<RdrFeedItem> unreadItems = from each in feed.Items
                                                   where each.Unread
                                                   select each;

            this.Items.AddMissingItems<RdrFeedItem>(unreadItems);
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

        private ObservableCollection<RdrFeed> _feeds = new ObservableCollection<RdrFeed>();
        public ObservableCollection<RdrFeed> Feeds { get { return this._feeds; } }

        private ObservableCollection<RdrFeedItem> _items = new ObservableCollection<RdrFeedItem>();
        public ObservableCollection<RdrFeedItem> Items { get { return this._items; } }
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

        private async void updateTimer_Tick(object sender, EventArgs e)
        {
            await RefreshAllFeedsAsync(null);
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

        private async Task WriteBasicFeedsFileAsync()
        {
            this.Activity = true;
            this.RaiseAllAsyncCanExecuteChangedCommands();

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<feeds>");
            sb.AppendLine("</feeds>");

            using (FileStream fsAsync = new FileStream(this.feedsFile, FileMode.Create, FileAccess.Write, FileShare.None, 1024, true))
            {
                using (StreamWriter sw = new StreamWriter(fsAsync))
                {
                    await sw.WriteAsync(sb.ToString()).ConfigureAwait(false);
                }
            }

            this.Activity = false;
            this.RaiseAllAsyncCanExecuteChangedCommands();
        }

        public async Task LoadAsync()
        {
            this.Activity = true;
            RaiseAllAsyncCanExecuteChangedCommands();

            IEnumerable<Uri> feedUris = await LoadXmlUrlsFromFileAsync();

            if (feedUris.Count<Uri>() > 0)
            {
                IEnumerable<RdrFeed> allFeeds = from each in feedUris
                                                select new RdrFeed(each);

                this.Feeds.AddList<RdrFeed>(allFeeds);
            }

            await RefreshAllFeedsAsync(null);

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

        private HttpWebRequest BuildHttpWebRequest(Uri xmlUrl)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(xmlUrl);

            req.AllowAutoRedirect = true;
            req.Host = xmlUrl.DnsSafeHost;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version11;
            req.Referer = string.Format("http://{0}/", xmlUrl.DnsSafeHost);
            req.Timeout = 5000;
            req.UserAgent = @"Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            req.Headers.Add("DNT", "1");

            return req;
        }

        private void MoveAllUnreadItemsToView(object _)
        {
            lock (this.Items)
            {
                this.Items.Clear();

                List<RdrFeedItem> toAdd = new List<RdrFeedItem>();

                foreach (RdrFeed each in this.Feeds)
                {
                    if (each != null)
                    {
                        if (each.Items != null)
                        {
                            toAdd.AddList<RdrFeedItem>(from eeach in each.Items
                                                       where eeach.Unread
                                                       select eeach);
                        }
                    }
                }

                this.Items.AddList<RdrFeedItem>(toAdd);
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
