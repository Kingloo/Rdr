using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                    this._refreshAllFeedsCommandAsync = new DelegateCommandAsync(new Func<Task>(RefreshAllFeedsAsync), canExecuteAsync);
                }

                return this._refreshAllFeedsCommandAsync;
            }
        }

        public async Task RefreshAllFeedsAsync()
        {
            this.Activity = true;
            this.WindowTitle = string.Format("{0} - updating...", appName);

            if (this.Feeds.Count > 0)
            {
                IEnumerable<Task> refreshTasks = from each in this.Feeds
                                                 select RefreshFeedAsync(each);

                await Task.WhenAll(refreshTasks);
            }

            this.WindowTitle = string.Format(appName);
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
                if (each != null)
                {
                    each.MarkAllItemsAsRead();
                }
            }

            MoveAllUnreadItemsToView();
        }

        private void MoveAllUnreadItemsToView()
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

        private DelegateCommand _moveUnreadItemsToViewCommand = null;
        public DelegateCommand MoveUnreadItemsToViewCommand
        {
            get
            {
                if (this._moveUnreadItemsToViewCommand == null)
                {
                    this._moveUnreadItemsToViewCommand = new DelegateCommand(new Action(MoveAllUnreadItemsToView), canExecute);
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
                Process.Start("notepad.exe", this.feedsFile);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show(string.Format("The feeds file:{0}{1}{0}was not found.", Environment.NewLine, this.feedsFile), "File Missing", MessageBoxButton.OK, MessageBoxImage.Error);
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
            //this.Feeds.Clear();

            await LoadFeedsFromFileAsync();
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
            Application.Current.MainWindow.Close();
        }
        #endregion

        #region Fields
        private const string appName = "Rdr";
        //private readonly string feedsFile = string.Format(@"C:\Users\{0}\Documents\rssfeeds.xml", Environment.UserName);
        private readonly string feedsFile = string.Format(@"C:\Users\{0}\Documents\RdrFeeds.txt", Environment.UserName);
        private readonly DispatcherTimer updateAllTimer = null;
        #endregion

        #region Properties
        private string _windowTitle = appName;
        public string WindowTitle
        {
            get
            {
                return this._windowTitle;
            }
            set
            {
                this._windowTitle = value;

                OnNotifyPropertyChanged();
            }
        }

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

                this.RaiseAllAsyncCanExecuteChangedCommands();
            }
        }

        private ObservableCollection<RdrFeed> _feeds = new ObservableCollection<RdrFeed>();
        public ObservableCollection<RdrFeed> Feeds { get { return this._feeds; } }

        private ObservableCollection<RdrFeedItem> _items = new ObservableCollection<RdrFeedItem>();
        public ObservableCollection<RdrFeedItem> Items { get { return this._items; } }
        #endregion

        public FeedManager()
        {
            this.updateAllTimer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 20, 0)
            };

            this.updateAllTimer.Tick += updateAllTimer_Tick;
            this.updateAllTimer.IsEnabled = true;

            // not awaited cos... ya can't
            Init();
        }

        private async void updateAllTimer_Tick(object sender, EventArgs e)
        {
            await RefreshAllFeedsAsync();
        }

        public async Task Init()
        {
            await LoadFeedsFromFileAsync();
            await RefreshAllFeedsAsync();
        }

        public async Task LoadFeedsFromFileAsync()
        {
            this.Activity = true;
            
            if (FeedsFileExists())
            {
                IEnumerable<RdrFeed> feeds = await ReadFromFeedsFileAsync();

                this.Feeds.AddMissingItems<RdrFeed>(feeds);

                List<RdrFeed> toBeDeleted = new List<RdrFeed>();
                foreach (RdrFeed each in this.Feeds)
                {
                    if (feeds.Contains<RdrFeed>(each) == false)
                    {
                        toBeDeleted.Add(each);
                    }
                }

                this.Feeds.RemoveList<RdrFeed>(toBeDeleted);
            }
            else
            {
                await WriteBasicFeedsFileAsync();
            }

            this.Activity = false;
        }

        private async Task<IEnumerable<RdrFeed>> ReadFromFeedsFileAsync()
        {
            List<RdrFeed> feeds = new List<RdrFeed>();

            using (FileStream fsAsync = new FileStream(this.feedsFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096, true))
            {
                using (StreamReader sr = new StreamReader(fsAsync))
                {
                    string line = string.Empty;

                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        // allows us to comment out a feed URL with a hash sign
                        if (line.StartsWith("#") == false)
                        {
                            Uri uri = null;

                            if (Uri.TryCreate(line, UriKind.Absolute, out uri))
                            {
                                RdrFeed feed = new RdrFeed(uri);

                                feeds.Add(feed);
                            }
                        }
                    }
                }
            }

            return feeds;
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

        private HttpWebRequest BuildHttpWebRequest(Uri xmlUrl)
        {
            HttpWebRequest req = HttpWebRequest.CreateHttp(xmlUrl);

            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            req.Host = xmlUrl.DnsSafeHost;
            req.KeepAlive = false;
            req.Method = "GET";
            req.ProtocolVersion = HttpVersion.Version11;
            req.Referer = string.Format("{0}://{1}/", xmlUrl.GetLeftPart(UriPartial.Scheme), xmlUrl.DnsSafeHost);
            req.Timeout = 2500;
            req.UserAgent = @"Mozilla/5.0 (Windows NT 6.3; Trident/7.0; rv:11.0) like Gecko";

            req.Headers.Add("DNT", "1");

            return req;
        }

        private bool canExecute(object _)
        {
            return true;
        }

        private bool canExecuteAsync(object _)
        {
            return !this.Activity;
        }

        private void RaiseAllAsyncCanExecuteChangedCommands()
        {
            this.RefreshAllFeedsCommandAsync.RaiseCanExecuteChanged();
            this.RefreshFeedCommandAsync.RaiseCanExecuteChanged();
            this.ReloadFeedsCommandAsync.RaiseCanExecuteChanged();
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
