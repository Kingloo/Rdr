using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Rdr.Common;
using RdrLib;
using RdrLib.Model;

namespace Rdr.Gui
{
	public class MainWindowViewModel : BindableBase, IMainWindowViewModel
	{
		private DelegateCommandAsync? refreshAllCommand = null;
		public DelegateCommandAsync RefreshAllCommand
		{
			get
			{
				refreshAllCommand ??= new DelegateCommandAsync(RefreshAllAsync, CanExecuteAsync);

				return refreshAllCommand;
			}
		}

		private DelegateCommandAsync<Feed>? refreshCommand = null;
		public DelegateCommandAsync<Feed> RefreshCommand
		{
			get
			{
				refreshCommand ??= new DelegateCommandAsync<Feed>(RefreshAsync, CanExecuteAsync);

				return refreshCommand;
			}
		}

		private DelegateCommand<Feed>? goToFeedCommand = null;
		public DelegateCommand<Feed> GoToFeedCommand
		{
			get
			{
				goToFeedCommand ??= new DelegateCommand<Feed>(GoToFeed, (_) => true);

				return goToFeedCommand;
			}
		}

		private DelegateCommand<Item>? goToItemCommand = null;
		public DelegateCommand<Item> GoToItemCommand
		{
			get
			{
				goToItemCommand ??= new DelegateCommand<Item>(GoToItem, (_) => true);

				return goToItemCommand;
			}
		}

		private DelegateCommand? markAsReadCommand = null;
		public DelegateCommand MarkAsReadCommand
		{
			get
			{
				markAsReadCommand ??= new DelegateCommand(MarkAllAsRead, (_) => true);

				return markAsReadCommand;
			}
		}

		private DelegateCommand? openFeedsFileCommand = null;
		public DelegateCommand OpenFeedsFileCommand
		{
			get
			{
				openFeedsFileCommand ??= new DelegateCommand(OpenFeedsFile, (_) => true);

				return openFeedsFileCommand;
			}
		}

		private DelegateCommandAsync? reloadCommand = null;
		public DelegateCommandAsync ReloadCommand
		{
			get
			{
				reloadCommand ??= new DelegateCommandAsync(ReloadAsync, CanExecuteAsync);

				return reloadCommand;
			}
		}

		private DelegateCommandAsync? seeUnreadCommand = null;
		public DelegateCommandAsync SeeUnreadCommand
		{
			get
			{
				seeUnreadCommand ??= new DelegateCommandAsync(SeeUnreadAsync, CanExecuteAsync);

				return seeUnreadCommand;
			}
		}

		private DelegateCommandAsync? seeAllCommand = null;
		public DelegateCommandAsync SeeAllCommand
		{
			get
			{
				seeAllCommand ??= new DelegateCommandAsync(SeeAllAsync, CanExecuteAsync);

				return seeAllCommand;
			}
		}

		private DelegateCommandAsync<Feed?>? viewFeedItemsCommand = null;
		public DelegateCommandAsync<Feed?> ViewFeedItemsCommand
		{
			get
			{
				viewFeedItemsCommand ??= new DelegateCommandAsync<Feed?>(ViewFeedItemsAsync, CanExecuteAsync);

				return viewFeedItemsCommand;
			}
		}

		public DelegateCommandAsync<Enclosure> DownloadEnclosureCommand
			=> new DelegateCommandAsync<Enclosure>(DownloadEnclosureAsync, CanExecuteAsync);

		private DelegateCommand<Window>? exitCommand = null;
		public DelegateCommand<Window> ExitCommand
		{
			get
			{
				exitCommand ??= new DelegateCommand<Window>(Exit);

				return exitCommand;
			}
		}

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

		private bool activity = false;
		public bool Activity
		{
			get => activity;
			set
			{
				SetProperty(ref activity, value, nameof(Activity));

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

		private string statusMessage = "no status message";
		public string StatusMessage
		{
			get => statusMessage;
			set => SetProperty(ref statusMessage, value, nameof(StatusMessage));
		}

		public bool IsRefreshTimerRunning
		{
			get => refreshTimer?.IsEnabled ?? false;
		}

		private int activeDownloads = 0;
		public bool HasActiveDownloads => activeDownloads > 0;

		private DispatcherTimer? refreshTimer = null;
		private Feed? selectedFeed = null;

		private readonly string feedsFilePath;

		private readonly IRdrService rdrService;
		public IRdrService RdrService { get => rdrService; }

		private readonly ObservableCollection<Item> viewedItems = new ObservableCollection<Item>();
		public IReadOnlyCollection<Item> ViewedItems { get => viewedItems; }

		public MainWindowViewModel(string feedsFilePath)
			: this(feedsFilePath, new RdrService())
		{ }

		public MainWindowViewModel(string feedsFilePath, IRdrService rdrService)
		{
			ArgumentNullException.ThrowIfNull(feedsFilePath);
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
			RefreshAllCommand.Execute();
		}

		private Task RefreshAllAsync()
			=> RefreshAsync(RdrService.Feeds);

		private async Task RefreshAsync(IEnumerable<Feed> feeds)
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

		private async Task RefreshAsync(Feed feed)
		{
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

					// we only want to remove the item if we are looking at unread items and items contains it
					if (selectedFeed is null && viewedItems.Contains(item))
					{
						viewedItems.Remove(item);
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
			if (selectedFeed is not null)
			{
				RdrService.MarkAsRead(selectedFeed);
			}
			else // selectedFeed is null means unread-view
			{
				RdrService.MarkAllAsRead();

				viewedItems.Clear();
			}
		}

		private void OpenFeedsFile()
		{
			if (!SystemLaunch.Path(feedsFilePath))
			{
				LogStatic.Message($"feeds file path does not exist ({feedsFilePath}), or process launch failed");
			}
		}

		private async Task ReloadAsync()
		{
			string[] lines = await ReadLinesAsync(feedsFilePath).ConfigureAwait(true);

			IReadOnlyCollection<Feed> feeds = CreateFeeds(lines);

			if (feeds.Count == 0)
			{
				RdrService.Clear();
				viewedItems.Clear();

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

		private async Task DownloadEnclosureAsync(Enclosure enclosure)
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

		private async Task ViewFeedItemsAsync(Feed? feed)
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

		private Task SeeUnreadAsync()
		{
			selectedFeed = null;

			return MoveUnreadItemsAsync(clearFirst: true);
		}

		private Task SeeAllAsync()
		{
			selectedFeed = null;

			viewedItems.Clear();

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
				viewedItems.Clear();
			}

			foreach (Item[] chunk in items.Chunk(50))
			{
				foreach (Item each in chunk)
				{
					if (!ViewedItems.Contains(each))
					{
						viewedItems.Add(each);
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

		public void Exit(Window window)
		{
			ArgumentNullException.ThrowIfNull(window);

			Web.DisposeHttpClient();

			window.Close();
		}
	}
}
