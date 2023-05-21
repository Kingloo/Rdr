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
				_refreshAllCommand ??= new DelegateCommandAsync(RefreshAllAsync, CanExecuteAsync);

				return _refreshAllCommand;
			}
		}

		private DelegateCommandAsync<Feed>? _refreshCommand = null;
		public DelegateCommandAsync<Feed> RefreshCommand
		{
			get
			{
				_refreshCommand ??= new DelegateCommandAsync<Feed>(RefreshAsync, CanExecuteAsync);

				return _refreshCommand;
			}
		}

		private DelegateCommand<Feed>? _goToFeedCommand = null;
		public DelegateCommand<Feed> GoToFeedCommand
		{
			get
			{
				_goToFeedCommand ??= new DelegateCommand<Feed>(GoToFeed, (_) => true);

				return _goToFeedCommand;
			}
		}

		private DelegateCommand<Item>? _goToItemCommand = null;
		public DelegateCommand<Item> GoToItemCommand
		{
			get
			{
				_goToItemCommand ??= new DelegateCommand<Item>(GoToItem, (_) => true);

				return _goToItemCommand;
			}
		}

		private DelegateCommand? _markAllAsReadCommand = null;
		public DelegateCommand MarkAllAsReadCommand
		{
			get
			{
				_markAllAsReadCommand ??= new DelegateCommand(MarkAllAsRead, (_) => true);

				return _markAllAsReadCommand;
			}
		}

		private DelegateCommand? _openFeedsFileCommand = null;
		public DelegateCommand OpenFeedsFileCommand
		{
			get
			{
				_openFeedsFileCommand ??= new DelegateCommand(OpenFeedsFile, (_) => true);

				return _openFeedsFileCommand;
			}
		}

		private DelegateCommandAsync? _reloadCommand = null;
		public DelegateCommandAsync ReloadCommand
		{
			get
			{
				_reloadCommand ??= new DelegateCommandAsync(ReloadAsync, CanExecuteAsync);

				return _reloadCommand;
			}
		}

        private DelegateCommandAsync? _seeUnreadCommand = null;
        public DelegateCommandAsync SeeUnreadCommand
        {
            get
            {
                _seeUnreadCommand ??= new DelegateCommandAsync(SeeUnreadAsync, CanExecuteAsync);

                return _seeUnreadCommand;
            }
        }

		private DelegateCommandAsync? _seeAllCommand = null;
        public DelegateCommandAsync SeeAllCommand
        {
            get
            {
                _seeAllCommand ??= new DelegateCommandAsync(SeeAllAsync, CanExecuteAsync);

                return _seeAllCommand;
            }
        }

        private DelegateCommandAsync<Feed?>? _viewFeedItemsCommand = null;
        public DelegateCommandAsync<Feed?> ViewFeedItemsCommand
        {
            get
            {
                _viewFeedItemsCommand ??= new DelegateCommandAsync<Feed?>(ViewFeedItemsAsync, CanExecuteAsync);

                return _viewFeedItemsCommand;
            }
        }

        public DelegateCommandAsync<Enclosure> DownloadEnclosureCommand
            => new DelegateCommandAsync<Enclosure>(DownloadEnclosureAsync, CanExecuteAsync);

		private bool CanExecuteAsync(object? obj)
        {
            if (obj is Enclosure enclosure)
            {
                return !enclosure.IsDownloading;
            }
            else
            {
                return !Activity;
            }
        }

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

        private void RaiseCanExecuteChangedOnAsyncCommands()
		{
			RefreshAllCommand.RaiseCanExecuteChanged();
			RefreshCommand.RaiseCanExecuteChanged();
			ReloadCommand.RaiseCanExecuteChanged();
            SeeUnreadCommand.RaiseCanExecuteChanged();
            SeeAllCommand.RaiseCanExecuteChanged();
            ViewFeedItemsCommand.RaiseCanExecuteChanged();
		}

		private string _statusMessage = "no status message";
		public string StatusMessage
		{
			get => _statusMessage;
			set => SetProperty(ref _statusMessage, value, nameof(StatusMessage));
		}

		public bool IsRefreshTimerRunning
        {
            get => refreshTimer?.IsEnabled ?? false;
        }

		private int activeDownloads = 0;
		public bool HasActiveDownload => activeDownloads > 0;

		private DispatcherTimer? refreshTimer = null;
		private Feed? selectedFeed = null;

        private readonly string feedsFilePath;

        private readonly IRdrService rdrService;
        public IRdrService RdrService { get => rdrService; }

        private readonly ObservableCollection<Item> vieweditems = new ObservableCollection<Item>();
        public IReadOnlyCollection<Item> ViewedItems { get => vieweditems; }

        public MainWindowViewModel(string feedsFilePath)
            : this(feedsFilePath, new RdrService())
        { }

        public MainWindowViewModel(string feedsFilePath, IRdrService rdrService)
		{
			if (String.IsNullOrWhiteSpace(feedsFilePath))
            {
                throw new ArgumentNullException(nameof(feedsFilePath));
            }

			ArgumentNullException.ThrowIfNull(rdrService);

            this.feedsFilePath = feedsFilePath;
			this.rdrService = rdrService;
		}

		public void StartTimer()
		{
			if (refreshTimer is not null)
            {
                return;
            }

            refreshTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
            {
                Interval = TimeSpan.FromMinutes(15d)
            };

            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
		}

		public void StopTimer()
		{
			if (refreshTimer is null)
            {
                return;
            }

            refreshTimer.Stop();
            refreshTimer.Tick -= RefreshTimer_Tick;
            refreshTimer = null;
		}

		private void RefreshTimer_Tick(object? sender, EventArgs e)
		{
			RefreshAllCommand.Execute(null);
		}

		public Task RefreshAllAsync() => RefreshAsync(RdrService.Feeds);
        
		public async Task RefreshAsync(IEnumerable<Feed> feeds)
		{
			Activity = true;

            StatusMessage = "updating ...";

			await RdrService.UpdateAsync(feeds).ConfigureAwait(true);

			if (selectedFeed is null)
			{
				await MoveUnreadItemsAsync(clearFirst: false).ConfigureAwait(true);
			}
			else
			{
				await MoveItemsAsync(selectedFeed).ConfigureAwait(true);
			}

			ShowLastUpdatedMessage();

			Activity = false;
		}

        public async Task RefreshAsync(Feed feed)
        {
			ArgumentNullException.ThrowIfNull(feed);

            Activity = true;
            
            await RdrService.UpdateAsync(feed).ConfigureAwait(true);

            if (selectedFeed is not null && selectedFeed == feed)
            {
                await MoveItemsAsync(feed).ConfigureAwait(true);
            }

            Activity = false;
        }

		private void GoToFeed(Feed feed)
		{
			if (feed.Link is Uri uri)
			{
				if (!SystemLaunch.Uri(uri))
				{
					LogStatic.Message($"feed link launch failed ({feed.Name})");
				}
			}
			else
			{
				LogStatic.Message($"feed link was null ({feed.Name})");
			}
		}

		private void GoToItem(Item item)
		{
			if (item.Link is Uri uri)
			{
				if (SystemLaunch.Uri(uri))
				{
					RdrService.MarkAsRead(item);

					// we only want to remove the item if we are looking at unread items and _items contains it
					if (selectedFeed is null && vieweditems.Contains(item))
					{
						vieweditems.Remove(item);
					}
				}
				else
				{
					LogStatic.Message($"item link launch failed ({item.Name})");
				}
			}
			else
			{
				LogStatic.Message($"item link was null ({item.Name})");
			}
		}

		private void MarkAllAsRead()
		{
			RdrService.MarkAllAsRead();

			if (selectedFeed is null)
			{
				vieweditems.Clear();
			}
		}

		public void OpenFeedsFile()
		{
			if (!SystemLaunch.Path(feedsFilePath))
			{
				LogStatic.Message($"feeds file path does not exist ({feedsFilePath}), or process launch failed");
			}
		}

		public async Task ReloadAsync()
		{
			string[] lines = await ReadLinesAsync(feedsFilePath).ConfigureAwait(true);

			IReadOnlyCollection<Feed> feeds = CreateFeeds(lines);

			if (feeds.Count == 0)
			{
				RdrService.Clear();
				vieweditems.Clear();

				return;
			}

			// something service.Feeds has that our loaded feeds doesn't
			var toRemove = RdrService.Feeds.Where(f => !feeds.Contains(f)).ToList();

			RdrService.Remove(toRemove);

			List<Feed> toRefresh = new List<Feed>();

			foreach (Feed[] chunkOfFeeds in feeds.Chunk(10))
			{
                foreach (Feed each in chunkOfFeeds)
                {
                    if (RdrService.Add(each))
                    {
                        toRefresh.Add(each);
                    }
                }

                await Dispatcher.Yield(DispatcherPriority.Background);
			}

			await RefreshAsync(toRefresh).ConfigureAwait(true);
		}

		private static async ValueTask<string[]> ReadLinesAsync(string path)
		{
			try
			{
				return await FileSystem.LoadLinesFromFileAsync(path).ConfigureAwait(false);
			}
			catch (FileNotFoundException)
			{
				await LogStatic.MessageAsync($"file not found: {path}").ConfigureAwait(false);

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
			ArgumentNullException.ThrowIfNull(enclosure);

			string profileFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string filename = enclosure.Link.Segments.Last();

			string path = Path.Combine(profileFolder, "share", filename);

			var progress = new Progress<FileProgress>(e =>
			{
				if (e.ContentLength.HasValue)
				{
					enclosure.Message = e.GetPercentFormatted(CultureInfo.CurrentCulture) ?? "error!";
				}
				else
				{
					enclosure.Message = $"{e.TotalBytesWritten} bytes written";
				}
			});

			enclosure.IsDownloading = true;
			activeDownloads++;

			FileResponse response = await RdrService.DownloadEnclosureAsync(enclosure, path, progress).ConfigureAwait(true);

			enclosure.IsDownloading = false;
			activeDownloads--;

			enclosure.Message = (response.Reason == Reason.Success) ? "Download" : response.Reason.ToString();
		}

		public async Task ViewFeedItemsAsync(Feed? feed)
		{
            if (feed is null)
            {
                selectedFeed = null;

                await MoveUnreadItemsAsync(clearFirst: true).ConfigureAwait(true);
            }
            else if (selectedFeed is null)
            {
                selectedFeed = feed;

                await MoveItemsAsync(feed.Items, clearFirst: true).ConfigureAwait(true);
            }
            else
            {
                if (feed != selectedFeed)
                {
                    selectedFeed = feed;

                    await MoveItemsAsync(feed.Items, clearFirst: true).ConfigureAwait(true);
                }
            }
		}

		public Task SeeUnreadAsync()
		{
            selectedFeed = null;

            return MoveUnreadItemsAsync(clearFirst: true);
		}

        public Task SeeAllAsync()
		{
			selectedFeed = null;

            vieweditems.Clear();

			var allItems = from feed in RdrService.Feeds
						   from item in feed.Items
						   orderby item.Published descending
						   select item;

			return MoveItemsAsync(allItems, clearFirst: true);
		}

		private Task MoveUnreadItemsAsync(bool clearFirst)
		{
			var unreadItems = from f in RdrService.Feeds
							  from i in f.Items
							  where i.Unread
							  select i;

			return MoveItemsAsync(unreadItems, clearFirst);
		}

        private Task MoveItemsAsync(Feed feed)
            => MoveItemsAsync(feed.Items, clearFirst: false);

        private async Task MoveItemsAsync(IEnumerable<Item> items, bool clearFirst)
		{
			if (clearFirst)
			{
                vieweditems.Clear();
			}

			foreach (Item[] chunk in items.Chunk(50))
			{
                foreach (Item each in chunk)
                {
                    if (!ViewedItems.Contains(each))
                    {
                        vieweditems.Add(each);
                    }
                }

                await Dispatcher.Yield(DispatcherPriority.Background);
			}
		}

		private void ShowLastUpdatedMessage()
		{
			string time = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

			StatusMessage = string.Format(CultureInfo.CurrentCulture, "last updated at {0}", time);
		}
	}
}
