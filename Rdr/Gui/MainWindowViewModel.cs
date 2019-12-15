using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using Rdr.Common;
using RdrLib;
using RdrLib.Model;

namespace Rdr.Gui
{
    public class MainWindowViewModel : BindableBase
    {
        private DelegateCommandAsync? _refreshAllCommand = null;
        public DelegateCommandAsync RefreshAllCommand
        {
            get
            {
                if (_refreshAllCommand is null)
                {
                    _refreshAllCommand = new DelegateCommandAsync(RefreshAllAsync, CanExecuteAsync);
                }

                return _refreshAllCommand;
            }
        }

        private DelegateCommandAsync<Feed>? _refreshCommand = null;
        public DelegateCommandAsync<Feed> RefreshCommand
        {
            get
            {
                if (_refreshCommand is null)
                {
                    _refreshCommand = new DelegateCommandAsync<Feed>(RefreshAsync, CanExecuteAsync);
                }

                return _refreshCommand;
            }
        }

        private DelegateCommand<Feed>? _goToFeedCommand = null;
        public DelegateCommand<Feed> GoToFeedCommand
        {
            get
            {
                if (_goToFeedCommand is null)
                {
                    _goToFeedCommand = new DelegateCommand<Feed>(GoToFeed, (_) => true);
                }

                return _goToFeedCommand;
            }
        }

        private DelegateCommand<Item>? _goToItemCommand = null;
        public DelegateCommand<Item> GoToItemCommand
        {
            get
            {
                if (_goToItemCommand is null)
                {
                    _goToItemCommand = new DelegateCommand<Item>(GoToItem, (_) => true);
                }

                return _goToItemCommand;
            }
        }

        private DelegateCommand? _markAllAsReadCommand = null;
        public DelegateCommand MarkAllAsReadCommand
        {
            get
            {
                if (_markAllAsReadCommand is null)
                {
                    _markAllAsReadCommand = new DelegateCommand(MarkAllAsRead, (_) => true);
                }

                return _markAllAsReadCommand;
            }
        }

        private DelegateCommand? _openFeedsFileCommand = null;
        public DelegateCommand OpenFeedsFileCommand
        {
            get
            {
                if (_openFeedsFileCommand is null)
                {
                    _openFeedsFileCommand = new DelegateCommand(OpenFeedsFile, (_) => true);
                }

                return _openFeedsFileCommand;
            }
        }

        private DelegateCommandAsync? _reloadCommand = null;
        public DelegateCommandAsync ReloadCommand
        {
            get
            {
                if (_reloadCommand is null)
                {
                    _reloadCommand = new DelegateCommandAsync(ReloadAsync, CanExecuteAsync);
                }

                return _reloadCommand;
            }
        }

        public DelegateCommandAsync<Enclosure> DownloadEnclosureCommand
            => new DelegateCommandAsync<Enclosure>(DownloadEnclosureAsync, CanExecuteAsync);

        private bool CanExecuteAsync(object _) => !Activity;

        private readonly DispatcherTimer refreshTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromMinutes(10d)
        };

        private readonly string feedsFilePath = string.Empty;
        private readonly RdrService service = new RdrService();
        private Feed? selectedFeed = null;

        public IReadOnlyCollection<Feed> Feeds => service.Feeds;

        private readonly ObservableCollection<Item> _items = new ObservableCollection<Item>();
        public IReadOnlyCollection<Item> Items => _items;

        public bool IsRefreshTimerRunning => refreshTimer.IsEnabled;

        private bool _activity = false;
        public bool Activity
        {
            get => _activity;
            set
            {
                SetProperty(ref _activity, value, nameof(Activity));

                RaiseCanExecuteChangedOnAsyncCommands();
            }
        }

        private int activeDownloads = 0;
        public bool HasActiveDownload => activeDownloads > 0;

        private void RaiseCanExecuteChangedOnAsyncCommands()
        {
            RefreshAllCommand.RaiseCanExecuteChanged();
            RefreshCommand.RaiseCanExecuteChanged();
            ReloadCommand.RaiseCanExecuteChanged();
        }

        public MainWindowViewModel(string feedsFilePath)
        {
            this.feedsFilePath = feedsFilePath;

            refreshTimer.Tick += RefreshTimer_Tick;
        }

        public void StartTimer()
        {
            if (!refreshTimer.IsEnabled)
            {
                refreshTimer.Start();
            }
        }

        public void StopTimer()
        {
            if (refreshTimer.IsEnabled)
            {
                refreshTimer.Stop();
            }
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            RefreshAllCommand.Execute(null);
        }

        public async Task RefreshAllAsync()
        {
            Activity = true;

            await service.UpdateAllAsync();

            if (selectedFeed is null)
            {
                MoveUnreadItems();
            }
            else
            {
                MoveItems(selectedFeed);
            }

            Activity = false;
        }

        public async Task RefreshAsync(Feed feed)
        {
            Activity = true;

            await service.UpdateAsync(feed);

            if (selectedFeed == feed)
            {
                MoveItems(feed);
            }

            Activity = false;
        }

        public async Task RefreshAsync(IEnumerable<Feed> feeds)
        {
            Activity = true;

            await service.UpdateAsync(feeds);

            if (selectedFeed is null)
            {
                MoveUnreadItems();
            }
            else
            {
                MoveItems(selectedFeed);
            }
            
            Activity = false;
        }

        private void GoToFeed(Feed feed)
        {
            if (feed.Link is Uri uri)
            {
                if (!SystemLaunch.Uri(uri))
                {
                    Log.Message($"feed link launch failed ({feed.Name})");
                }
            }
            else
            {
                Log.Message($"feed link was null ({feed.Name})");
            }
        }

        private void GoToItem(Item item)
        {
            if (item.Link is Uri uri)
            {
                if (SystemLaunch.Uri(uri))
                {
                    service.MarkAsRead(item);

                    if ((selectedFeed is null) && (_items.Contains(item)))
                    {
                        _items.Remove(item);
                    }
                }
                else
                {
                    Log.Message($"item link launch failed ({item.Name})");
                }
            }
            else
            {
                Log.Message($"item link was null ({item.Name})");
            }
        }

        private void MarkAllAsRead()
        {
            service.MarkAllAsRead();

            if (selectedFeed is null)
            {
                _items.Clear();
            }
        }

        public void OpenFeedsFile()
        {
            if (!SystemLaunch.Path(feedsFilePath))
            {
                Log.Message($"feeds file path does not exist ({feedsFilePath}), or process launch failed");
            }
        }

        public async Task ReloadAsync()
        {
            string[] lines = await ReadLinesAsync(feedsFilePath, "#");

            IReadOnlyCollection<Feed> feeds = CreateFeeds(lines);

            if (feeds.Count == 0)
            {
                service.Clear();
                _items.Clear();

                return;
            }

            // something service.Feeds has that our loaded feeds doesn't
            var toRemove = service.Feeds.Where(f => !feeds.Contains(f)).ToList();

            service.Remove(toRemove);

            List<Feed> toRefresh = new List<Feed>();

            foreach (Feed feed in feeds)
            {
                if (service.Add(feed))
                {
                    toRefresh.Add(feed);
                }
            }

            await RefreshAsync(toRefresh);
        }

        private static async Task<string[]> ReadLinesAsync(string path, string commentChar)
        {
            try
            {
                return await FileSystem.LoadLinesFromFileAsync(path, commentChar).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                await Log.MessageAsync($"file not found: {path}").ConfigureAwait(false);

                return Array.Empty<string>();
            }
        }

        private static IReadOnlyCollection<Feed> CreateFeeds(string[] lines)
        {
            List<Feed> feeds = new List<Feed>();

            foreach (string line in lines)
            {
                if (Uri.TryCreate(line, UriKind.Absolute, out Uri? uri))
                {
                    Feed feed = new Feed(uri);

                    feeds.Add(feed);
                }
            }

            return feeds.AsReadOnly();
        }

        public async Task DownloadEnclosureAsync(Enclosure enclosure)
        {
            string directory = @"C:\Users\k1ngl\share";
            string filename = enclosure.Link.Segments.Last();

            string path = Path.Combine(directory, filename);

            var progress = new Progress<DownloadProgress>(e =>
            {
                if (e.ContentLength.HasValue)
                {
                    decimal current = Convert.ToDecimal(e.TotalBytesReceived);
                    decimal total = Convert.ToDecimal(e.ContentLength.Value);

                    decimal percent = current / total;

                    enclosure.Message = percent.ToString(GetPercentFormat());
                }
                else
                {
                    enclosure.Message = $"{e.TotalBytesReceived} bytes received";
                }
            });

            enclosure.IsDownloading = true;
            activeDownloads++;

            DownloadResult result = await service.DownloadEnclosureAsync(enclosure, path, progress);

            enclosure.IsDownloading = false;
            activeDownloads--;

            enclosure.Message = (result == DownloadResult.Success) ? "Download" : result.ToString();
        }

        private static string GetPercentFormat()
        {
            var cc = CultureInfo.CurrentCulture;

            string separator = cc.NumberFormat.PercentDecimalSeparator;
            string symbol = cc.NumberFormat.PercentSymbol;

            return String.Format(cc, "0{0}0 {1}", separator, symbol);
        }

        public void SetSelectedFeed(Feed? feed)
        {
            if (feed is null)
            {
                selectedFeed = null;

                MoveUnreadItems();
            }
            else
            {
                selectedFeed = feed;

                MoveItems(selectedFeed.Items, clearFirst: true);
            }
        }

        private void MoveUnreadItems()
        {
            var unreadItems = from f in Feeds
                              from i in f.Items
                              where i.Unread
                              select i;

            MoveItems(unreadItems, clearFirst: true);
        }

        private void MoveItems(Feed feed) => MoveItems(feed.Items, clearFirst: false);

        private void MoveItems(IEnumerable<Item> items, bool clearFirst)
        {
            if (clearFirst)
            {
                _items.Clear();
            }

            foreach (Item item in items)
            {
                if (!_items.Contains(item))
                {
                    _items.Add(item);
                }
            }
        }

        public void CleanUp()
        {
            service.CleanUp();
        }
    }
}