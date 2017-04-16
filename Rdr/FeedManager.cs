using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Rdr.Extensions;
using Rdr.Model;

using static System.FormattableString;

namespace Rdr
{
    public class FeedManager : ViewModelBase
    {
        #region Events
        public event EventHandler FeedChanged;

        private void OnFeedChanged(RdrFeed feed)
            => FeedChanged?.Invoke(feed, new EventArgs());
        #endregion

        #region Commands
        private DelegateCommandAsync _refreshAllFeedsCommandAsync = null;
        public DelegateCommandAsync RefreshAllFeedsCommandAsync
        {
            get
            {
                if (_refreshAllFeedsCommandAsync == null)
                {
                    _refreshAllFeedsCommandAsync = new DelegateCommandAsync(RefreshAllFeedsAsync, CanExecuteAsync);
                }

                return _refreshAllFeedsCommandAsync;
            }
        }

        public async Task RefreshAllFeedsAsync()
        {
            if (Feeds.Count > 0)
            {
                var refreshTasks = Feeds
                    .Where(x => !x.Updating)
                    .Select(x => RefreshFeedAsync(x));

                await Task.WhenAll(refreshTasks)
                    .ConfigureAwait(false);
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
            if (feed.Name.Equals("Unread")) { return; }

            feed.Updating = true;
            Activity = activeTasks.Any();

            string websiteAsString = await GetFeed(feed.XmlUrl);

            if (String.IsNullOrWhiteSpace(websiteAsString))
            {
                feed.Updating = false;
                Activity = activeTasks.Any();

                return;
            }

            // removing this breaks something
            //websiteAsString = websiteAsString.Replace((char)(0x1F), (char)(0x20));

            XDocument x = ParseIntoXDocument(websiteAsString, feed.XmlUrl);

            if (x == null)
            {
                feed.Updating = false;
                Activity = activeTasks.Any();

                return;
            }

            feed.Load(x);

            AddUnreadItemsToUnreadCollector(feed.Items);

            feed.Updating = false;
            Activity = activeTasks.Any();
        }

        private static async Task<string> GetFeed(Uri uri)
        {
            return await Download.WebsiteAsync(uri)
                .ConfigureAwait(false);
        }

        private static XDocument ParseIntoXDocument(string websiteAsString, Uri feedUri)
        {
            XDocument x = null;

            try
            {
                x = XDocument.Parse(websiteAsString);
            }
            catch (XmlException ex)
            {
                string errorMessage = Invariant($"Parsing to XDocument failed: {feedUri.AbsoluteUri}");

                Log.LogException(ex, errorMessage);
            }

            return x;
        }

        private void AddUnreadItemsToUnreadCollector(IEnumerable<RdrFeedItem> feedItems)
        {
            var unreadItems = feedItems.Where(x => x.Unread);
            
            _feeds.SortFirst.AddMissingItems(unreadItems);
        }

        private DelegateCommand _markAllItemsAsReadCommand = null;
        public DelegateCommand MarkAllItemsAsReadCommand
        {
            get
            {
                if (_markAllItemsAsReadCommand == null)
                {
                    _markAllItemsAsReadCommand = new DelegateCommand(MarkAllItemsAsRead, CanExecute);
                }

                return _markAllItemsAsReadCommand;
            }
        }

        private void MarkAllItemsAsRead()
        {
            _feeds.SortFirst.ClearItems();

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
            
            if (_feeds.SortFirst.Items.Contains(item))
            {
                _feeds.SortFirst.RemoveItem(item);
            }
        }

        private DelegateCommand<RdrFeed> _goToFeedCommand = null;
        public DelegateCommand<RdrFeed> GoToFeedCommand
        {
            get
            {
                if (_goToFeedCommand == null)
                {
                    _goToFeedCommand = new DelegateCommand<RdrFeed>(GoToFeed, CanExecute);
                }

                return _goToFeedCommand;
            }
        }

        private void GoToFeed(RdrFeed feed) => Utils.OpenUriInBrowser(feed.XmlUrl);

        private DelegateCommand<RdrFeedItem> _goToItemCommand = null;
        public DelegateCommand<RdrFeedItem> GoToItemCommand
        {
            get
            {
                if (_goToItemCommand == null)
                {
                    _goToItemCommand = new DelegateCommand<RdrFeedItem>(GoToItem, CanExecute);
                }

                return _goToItemCommand;
            }
        }

        private void GoToItem(RdrFeedItem feedItem)
        {
            if (feedItem.Link != null)
            {
                Utils.OpenUriInBrowser(feedItem.Link);
            }
            else
            {
                Log.LogMessage("link was null");
            }

            MarkItemAsRead(feedItem);
        }

        private DelegateCommand<RdrFeed> _moveItemsToViewCommand = null;
        public DelegateCommand<RdrFeed> MoveItemsToViewCommand
        {
            get
            {
                if (_moveItemsToViewCommand == null)
                {
                    _moveItemsToViewCommand = new DelegateCommand<RdrFeed>(MoveItemsToView, CanExecute);
                }

                return _moveItemsToViewCommand;
            }
        }

        public void MoveItemsToView(RdrFeed feed) => OnFeedChanged(feed);

        private DelegateCommand _openFeedsFileCommand = null;
        public DelegateCommand OpenFeedsFileCommand
        {
            get
            {
                if (_openFeedsFileCommand == null)
                {
                    _openFeedsFileCommand = new DelegateCommand(OpenFeedsFile, CanExecute);
                }

                return _openFeedsFileCommand;
            }
        }

        private void OpenFeedsFile()
        {
            try
            {
                Process.Start("notepad.exe", feedsRepo.FilePath);
            }
            catch (FileNotFoundException)
            {
                Process.Start(feedsRepo.FilePath);
            }
        }

        private DelegateCommandAsync _loadFeedsCommandAsync = null;
        public DelegateCommandAsync LoadFeedsCommandAsync
        {
            get
            {
                if (_loadFeedsCommandAsync == null)
                {
                    _loadFeedsCommandAsync = new DelegateCommandAsync(LoadFeedsAsync, CanExecuteAsync);
                }

                return _loadFeedsCommandAsync;
            }
        }

        public async Task LoadFeedsAsync()
        {
            var feedUris = await feedsRepo.LoadAsync();

            var feeds = feedUris
                .Select(x => new RdrFeed(x))
                .ToList();
            
            _feeds.AddMissing(feeds);
            
            var toBeRemoved = Feeds
                .Where(x => !feeds.Contains(x) && !x.Name.Equals("Unread"))
                .ToList();
            
            _feeds.RemoveRange(toBeRemoved);
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

        private async Task DownloadEnclosureAsync(RdrEnclosure enclosure)
        {
            if (enclosure.DownloadLink == null)
            {
                Log.LogMessage($"{enclosure.Parent.Name}: enclosure download link is null");

                return;
            }

            string format = GetPercentFormat();

            FileInfo file = DetermineLocalFile(enclosure.DownloadLink);
            
            Download dl = new Download(enclosure.DownloadLink, file, 1024 * 256); // 512 KiB

            dl.DownloadProgress += (s, e) =>
            {
                Utils.DispatchSafely(Dispatcher.CurrentDispatcher, () =>
                {
                    enclosure.ButtonText = e.Percent.ToString(format);
                },
                DispatcherPriority.ApplicationIdle);
            };

            enclosure.Downloading = true;

            var result = await dl.ToFileAsync(false);

            enclosure.Downloading = false;

            switch (result)
            {
                case DownloadResult.Success:
                    enclosure.ButtonText = "Downloaded";
                    break;
                case DownloadResult.UriError:
                    enclosure.ButtonText = "Link error";
                    break;
                case DownloadResult.FileAlreadyExists:
                    enclosure.ButtonText = "File already exists";
                    break;
                default:
                    enclosure.ButtonText = "Error";
                    break;
            }
        }
        
        private FileInfo DetermineLocalFile(Uri uri)
        {
            string filename = uri.Segments.Last();

            return new FileInfo(Path.Combine(downloadDirectory, filename));
        }

        private static string GetPercentFormat()
        {
            NumberFormatInfo numberFormat = CultureInfo.CurrentCulture.NumberFormat;

            string separator = numberFormat.PercentDecimalSeparator;
            string symbol = numberFormat.PercentSymbol;

            return $"0{separator}0 {symbol}";
        }

        private bool CanExecute(object _) => true;

        private bool CanExecuteAsync(object _) => !Activity;
        #endregion

        #region Fields
        private const string appName = "Rdr";
        private readonly IRepo feedsRepo = null;
        private IEnumerable<RdrFeed> activeTasks;

        private readonly string downloadDirectory
            = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                @"share");

        private readonly DispatcherTimer updateAllTimer = new DispatcherTimer
        {
            Interval = new TimeSpan(0, 25, 0)
        };
        #endregion

        #region Properties
        public string WindowTitle
            => Activity
            ? string.Format(CultureInfo.CurrentCulture, "{0} - updating", appName)
            : appName;

        private bool _activity = false;
        public bool Activity
        {
            get
            {
                return this._activity;
            }
            set
            {
                if (_activity != value)
                {
                    _activity = value;

                    RaisePropertyChanged(nameof(Activity));
                    RaisePropertyChanged(nameof(WindowTitle));

                    RaiseAllAsyncCanExecuteChangedCommands();
                }
            }
        }

        private void RaiseAllAsyncCanExecuteChangedCommands()
        {
            RefreshAllFeedsCommandAsync.RaiseCanExecuteChanged();
            RefreshFeedCommandAsync.RaiseCanExecuteChanged();
            LoadFeedsCommandAsync.RaiseCanExecuteChanged();
        }
        
        private readonly ObservableSortingCollection<RdrFeed> _feeds
            = new ObservableSortingCollection<RdrFeed>(new RdrFeed("Unread"));
        public ObservableSortingCollection<RdrFeed> Feeds => _feeds;
        #endregion

        public FeedManager(IRepo feedsRepo)
        {
            this.feedsRepo = feedsRepo;

            activeTasks = Feeds.Where(x => x.Updating);
            
            _feeds.CollectionChanged += Feeds_CollectionChanged;
            
            updateAllTimer.Tick += UpdateAllTimer_Tick;
            updateAllTimer.Start();
        }
        
        private async void Feeds_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var refreshTasks = e.NewItems.Cast<RdrFeed>()
                    .Where(x => !x.Updating)
                    .Select(x => RefreshFeedAsync(x));

                await Task.WhenAll(refreshTasks)
                    .ConfigureAwait(false);
            }

            _feeds.DoSorting();
        }

        private async void UpdateAllTimer_Tick(object sender, EventArgs e)
            => await RefreshAllFeedsAsync();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetType().ToString());
            sb.AppendLine(Invariant($"Feeds: {Feeds.Count}"));

            return sb.ToString();
        }
    }
}
