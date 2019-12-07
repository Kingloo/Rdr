using Rdr.Common;
using RdrLib;
using RdrLib.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Rdr.Gui
{
    public class MainWindowViewModel : BindableBase
    {
        private DelegateCommandAsync? _refreshAllCommand;
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

        private DelegateCommand? _markAllAsReadCommand;
        public DelegateCommand MarkAllAsReadCommand
        {
            get
            {
                if (_markAllAsReadCommand is null)
                {
                    _markAllAsReadCommand = new DelegateCommand(service.MarkAllAsRead, (_) => true);
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

        private bool CanExecuteAsync(object _)
        {
            return true;
        }

        private readonly DispatcherTimer refreshTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
        {
            Interval = TimeSpan.FromMinutes(15d)
        };

        private readonly string feedsFilePath = string.Empty;
        private readonly RdrService service = new RdrService();

        public IReadOnlyCollection<Feed> Feeds => service.Feeds;

        public bool IsTimerRunning => refreshTimer.IsEnabled;

        private bool _activity = false;
        public bool Activity
        {
            get => _activity;
            set => SetProperty(ref _activity, value, nameof(Activity));
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
            await service.UpdateAllAsync();
        }

        public async Task RefreshAsync(Feed feed)
        {
            await service.UpdateAsync(feed);
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

            await service.UpdateAsync(toRefresh);
        }

        private static async Task<string[]> ReadLinesAsync(string path, string commentChar)
        {
            string[] lines = Array.Empty<string>();

            try
            {
                lines = await FileSystem.LoadLinesFromFileAsync(path, commentChar).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                await Log.MessageAsync($"file not found: {path}").ConfigureAwait(false);
            }

            return lines;
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
    }
}