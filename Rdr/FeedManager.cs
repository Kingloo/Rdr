using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using Rdr.Common;
using Rdr.DataAccess;
using Rdr.Extensions;
using Rdr.Model;

namespace Rdr
{
    public class FeedManager : ViewModelBase
    {
        #region Events
        public event EventHandler FeedChanged;

        private void OnFeedChanged(RdrFeed feed) => FeedChanged?.Invoke(feed, new EventArgs());
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

                await Task.WhenAll(refreshTasks).ConfigureAwait(false);
            }   
        }

        // we do not cache so that we can refresh a second feed manually before the first one has finished
        public DelegateCommandAsync<RdrFeed> RefreshFeedCommandAsync
        {
            get => new DelegateCommandAsync<RdrFeed>(RefreshFeedAsync, _ => true);
        }

        private async Task RefreshFeedAsync(RdrFeed feed)
        {
            if (feed.Name.Equals("Unread")) { return; }

            feed.Updating = true;
            Activity = activeTasks.Any();
            
            string website = await Download.WebsiteAsync(feed.XmlUrl);

            if (!String.IsNullOrWhiteSpace(website))
            {
                // removing this breaks something
                //websiteAsString = websiteAsString.Replace((char)(0x1F), (char)(0x20));

                if (TryParseXml(website, feed.XmlUrl, out XDocument x))
                {
                    feed.Load(x);

                    AddUnreadItemsToUnreadCollector(feed.Items);
                }
            }

            _feeds.DoSorting();
            
            feed.Updating = false;
            Activity = activeTasks.Any();
        }

        private static bool TryParseXml(string raw, Uri feedUri, out XDocument document)
        {
            if (String.IsNullOrWhiteSpace(raw))
            {
                document = null;
                return false;
            }

            if (feedUri is null)
            {
                document = null;
                return false;
            }

            try
            {
                document = XDocument.Parse(raw);
                return true;
            }
            catch (XmlException ex)
            {
                var cc = CultureInfo.CurrentCulture;
                string link = feedUri.AbsoluteUri;

                string errorMessage = string.Format(cc, "Parsing of XML document failed: {0}", link);

                Log.Exception(ex, errorMessage);
            }

            document = null;
            return false;
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

        private void GoToFeed(RdrFeed feed) => feed.XmlUrl.OpenInBrowser();

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
            if (feedItem is null) { throw new ArgumentNullException(nameof(feedItem)); }

            if (feedItem.Link is Uri uri)
            {
                uri.OpenInBrowser();
            }
            else
            {
                Log.Message($"link from {feedItem.Name} was null");
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

        private void OpenFeedsFile() => feedsFile.Launch();
        
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
            string[] feedUris = await FileSystem.GetLinesAsync(feedsFile, true);

            var feedsOnThisLoad = new List<RdrFeed>();

            foreach (string each in feedUris)
            {
                if (each.StartsWith("#"))
                {
                    continue;
                }

                if (Uri.TryCreate(each, UriKind.Absolute, out Uri uri))
                {
                    feedsOnThisLoad.Add(new RdrFeed(uri));
                }
            }

            _feeds.AddMissing(feedsOnThisLoad);
            
            var toBeRemoved = Feeds
                .Where(x => !feedsOnThisLoad.Contains(x) && !x.Name.Equals("Unread"))
                .ToList();
            
            _feeds.RemoveRange(toBeRemoved);

            RemoveAFeedsUnreadItemsFromCollector(toBeRemoved);
        }

        private void RemoveAFeedsUnreadItemsFromCollector(List<RdrFeed> toBeRemoved)
        {
            foreach (var feed in toBeRemoved)
            {
                foreach (var item in feed.Items)
                {
                    if (_feeds.SortFirst.Items.Contains(item))
                    {
                        _feeds.SortFirst.RemoveItem(item);
                    }
                }
            }
        }

        // we deliberately don't cache this download command so that each enclosure gets its own
        // otherwise starting a single download would prevent starting any other
        public DelegateCommandAsync<RdrEnclosure> DownloadEnclosureCommandAsync
        {
            get => new DelegateCommandAsync<RdrEnclosure>(DownloadEnclosureAsync, enclosure => !enclosure.Downloading);
        }

        private async Task DownloadEnclosureAsync(RdrEnclosure enclosure)
        {
            if (enclosure.DownloadLink is null)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, "{0} - download link is null", enclosure.Parent.Name);

                await Log.MessageAsync(errorMessage).ConfigureAwait(false);

                return;
            }

            FileInfo file = DetermineLocalFile(enclosure.DownloadLink);
            
            var download = new Download(enclosure.DownloadLink, file);
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.ContentLength.HasValue)
                {
                    decimal current = Convert.ToDecimal(p.TotalBytesReceived);
                    decimal total = Convert.ToDecimal(p.ContentLength);

                    decimal percent = current / total;

                    enclosure.ButtonText = percent.ToString(GetPercentFormat());
                }
                else
                {
                    enclosure.ButtonText = p.TotalBytesReceived.ToString(CultureInfo.CurrentUICulture);
                }
            });

            enclosure.Downloading = true;

            var result = await download.ToFileAsync(progress);

            enclosure.Downloading = false;

            switch (result)
            {
                case DownloadResult.Success:
                    enclosure.ButtonText = "Downloaded";
                    break;
                case DownloadResult.InternetError:
                    enclosure.ButtonText = "Internet error";
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
            string filePath = Path.Combine(downloadDir.FullName, filename);

            return new FileInfo(filePath);
        }

        private static string GetPercentFormat()
        {
            var cc = CultureInfo.CurrentCulture;

            string separator = cc.NumberFormat.PercentDecimalSeparator;
            string symbol = cc.NumberFormat.PercentSymbol;

            return string.Format(cc, "0{0}0 {1}", separator, symbol);
        }

        private bool CanExecute(object _) => true;

        private bool CanExecuteAsync(object _) => !Activity;
        #endregion

        #region Fields
        private const string appName = "Rdr";
        private readonly FileInfo feedsFile = default;
        private readonly DirectoryInfo downloadDir = default;
        private IEnumerable<RdrFeed> activeTasks = default;

        private readonly DispatcherTimer updateAllTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(15d)
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
            get => _activity;
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

        public FeedManager(FileInfo file, DirectoryInfo directory)
        {
            feedsFile = file ?? throw new ArgumentNullException(nameof(file));
            downloadDir = directory ?? throw new ArgumentNullException(nameof(directory));

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
                    .Select(x => RefreshFeedAsync(x))
                    .ToList();

                await Task.WhenAll(refreshTasks).ConfigureAwait(false);
            }
        }

        private async void UpdateAllTimer_Tick(object sender, EventArgs e)
            => await RefreshAllFeedsAsync();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(GetType().FullName);
            sb.Append("Feeds: ");
            sb.AppendLine(Feeds.Count.ToString(CultureInfo.CurrentCulture));

            return sb.ToString();
        }
    }
}
