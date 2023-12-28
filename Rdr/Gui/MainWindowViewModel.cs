using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rdr.Common;
using RdrLib;
using RdrLib.Model;
using static Rdr.EventIds.MainWindowViewModel;
using static Rdr.Gui.MainWindowViewModelLoggerMessages;

namespace Rdr.Gui
{
	public partial class MainWindowViewModel : BindableBase, IMainWindowViewModel
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
			return obj switch
			{
				Enclosure enclosure => !enclosure.IsDownloading,
				_ => !Activity
			};
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
			get => updateTimer?.IsEnabled ?? false;
		}

		private int activeDownloads = 0;
		public bool HasActiveDownloads => activeDownloads > 0;

		private readonly ObservableCollection<Item> viewedItems = new ObservableCollection<Item>();
		public IReadOnlyCollection<Item> ViewedItems { get => viewedItems; }

		private readonly IRdrService rdrService;
		public IRdrService RdrService { get => rdrService; }

		private readonly IOptionsMonitor<RdrOptions> rdrOptionsMonitor;
		private readonly ILogger<MainWindowViewModel> logger;

		private DispatcherTimer? updateTimer = null;
		private Feed? selectedFeed = null;

		private static readonly string defaultFeedsFilePath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
			"RdrFeeds.txt");

		private static readonly string defaultDownloadDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			"Downloads");

		public MainWindowViewModel(
			IRdrService rdrService,
			IOptionsMonitor<RdrOptions> rdrOptionsMonitor,
			ILogger<MainWindowViewModel> logger)
		{
			ArgumentNullException.ThrowIfNull(rdrService);
			ArgumentNullException.ThrowIfNull(rdrOptionsMonitor);
			ArgumentNullException.ThrowIfNull(logger);

			this.rdrService = rdrService;
			this.rdrOptionsMonitor = rdrOptionsMonitor;
			this.logger = logger;
		}

		public void StartTimer()
		{
			if (updateTimer is not null)
			{
				return;
			}

			updateTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
			{
				Interval = rdrOptionsMonitor.CurrentValue.UpdateInterval
			};

			updateTimer.Tick += RefreshTimer_Tick;
			updateTimer.Start();
		}

		public void StopTimer()
		{
			if (updateTimer is null)
			{
				return;
			}

			updateTimer.Stop();
			updateTimer.Tick -= RefreshTimer_Tick;
			updateTimer = null;
		}

		private void RefreshTimer_Tick(object? sender, EventArgs e)
		{
			RefreshAllCommand.Execute();
		}

		private Task RefreshAllAsync()
			=> RefreshAsync(rdrService.Feeds);

		private async Task RefreshAsync(IEnumerable<Feed> feeds)
		{
			Activity = true;

			StatusMessage = "updating ...";

			await rdrService.UpdateAsync(feeds).ConfigureAwait(true);

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

			await rdrService.UpdateAsync(feed).ConfigureAwait(true);

			if (selectedFeed is not null && selectedFeed == feed)
			{
				await MoveItemsAsync(feed).ConfigureAwait(true);
			}

			Activity = false;
		}

		private void GoToFeed(Feed feed)
		{
			if (feed.Link is null)
			{
				LogGoToFeedLinkNull(logger, feed.Name);

				return;
			}

			if (!SystemLaunch.Uri(feed.Link))
			{
				LogGoToFeedFailed(logger, feed.Name);

				return;
			}

			LogGoToFeed(logger, feed.Name);
		}

		private void GoToItem(Item item)
		{
			if (item.Link is null)
			{
				LogGoToItemLinkNull(logger, item.FeedName, item.Name);

				return;
			}

			if (!SystemLaunch.Uri(item.Link))
			{
				LogGoToItemFailed(logger, item.Link.AbsoluteUri);

				return;
			}

			rdrService.MarkAsRead(item);

			// we only want to remove the item if we are looking at unread items and items contains it
			if (selectedFeed is null && viewedItems.Contains(item))
			{
				viewedItems.Remove(item);
			}

			LogGoToItem(logger, item.Name);
		}

		private void MarkAllAsRead()
		{
			if (selectedFeed is not null)
			{
				rdrService.MarkAsRead(selectedFeed);
			}
			else // selectedFeed is null means unread-view
			{
				rdrService.MarkAllAsRead();

				viewedItems.Clear();
			}
		}

		private void OpenFeedsFile()
		{
			string currentFeedsFilePath = DetermineFeedsFileFullPath(rdrOptionsMonitor.CurrentValue);

			if (SystemLaunch.Path(currentFeedsFilePath))
			{
				LogFeedsFileOpened(logger, currentFeedsFilePath);
			}
			else
			{
				LogFeedsFileError(logger, currentFeedsFilePath);
			}
		}

		private async Task ReloadAsync()
		{
			string currentFeedsFilePath = DetermineFeedsFileFullPath(rdrOptionsMonitor.CurrentValue);

			LogReloadFeedsFileStarted(logger, currentFeedsFilePath);

			string[] lines = await ReadLinesAsync(currentFeedsFilePath).ConfigureAwait(true);

			IReadOnlyCollection<Feed> feeds = CreateFeeds(lines);

			if (feeds.Count == 0)
			{
				LogFeedsFileEmpty(logger, currentFeedsFilePath);

				rdrService.ClearFeeds();

				viewedItems.Clear();

				return;
			}

			// something service.Feeds has that our loaded feeds doesn't
			var toRemove = rdrService.Feeds.Where(f => !feeds.Contains(f)).ToList();

			rdrService.Remove(toRemove);

			List<Feed> toRefresh = new List<Feed>();

			foreach (Feed[] chunkOfFeeds in feeds.Chunk(10))
			{
				foreach (Feed each in chunkOfFeeds)
				{
					if (rdrService.Add(each))
					{
						toRefresh.Add(each);
					}
				}

				await Dispatcher.Yield(DispatcherPriority.Background);
			}

			LogReloadFeedsFileFinished(logger, currentFeedsFilePath);

			await RefreshAsync(toRefresh).ConfigureAwait(true);
		}

		private async ValueTask<string[]> ReadLinesAsync(string path)
		{
			try
			{
				return await FileSystem.LoadLinesFromFileAsync(path).ConfigureAwait(false);
			}
			catch (FileNotFoundException ex)
			{
				LogFeedsFileDoesNotExist(logger, ex.FileName ?? "empty", path);

				return Array.Empty<string>();
			}
		}

		private static ReadOnlyCollection<Feed> CreateFeeds(string[] lines)
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

			string downloadDirectory = DetermineDownloadDirectory(rdrOptionsMonitor.CurrentValue);
			string filename = DetermineFileName(enclosure);
			string fullPath = Path.Combine(downloadDirectory, filename);

			LogDownloadLocalFilePath(logger, enclosure.Link.AbsoluteUri, fullPath);

			Progress<FileProgress> progress = new Progress<FileProgress>((FileProgress e) =>
			{
				string message = e.ContentLength.HasValue
					? e.GetPercentFormatted(CultureInfo.CurrentCulture) ?? "error!"
					: $"{e.TotalBytesWritten} bytes written";

				enclosure.Message = message;

				LogDownloadProgress(logger, enclosure.Link.AbsoluteUri, fullPath, message);
			});

			enclosure.IsDownloading = true;
			activeDownloads++;

			LogDownloadStarted(logger, enclosure.Link.AbsoluteUri, fullPath);

			FileResponse response = await rdrService.DownloadEnclosureAsync(enclosure, fullPath, progress).ConfigureAwait(true);

			if (response.Reason == Reason.Success)
			{
				LogDownloadFinished(logger, enclosure.Link.AbsoluteUri, fullPath);
			}
			else
			{
				LogDownloadFailed(logger, response.Reason, response.StatusCode, enclosure.Link.AbsoluteUri);
			}

			enclosure.IsDownloading = false;
			activeDownloads--;

			enclosure.Message = (response.Reason == Reason.Success) ? "Download" : response.Reason.ToString();
		}

		private static string DetermineDownloadDirectory(RdrOptions rdrOptions)
		{
			return rdrOptions.DownloadDirectory switch
			{
				string { Length: > 0 } value => Path.IsPathRooted(value)
					? Directory.Exists(value)
						? value
						: defaultDownloadDirectory
					: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), value),
				_ => defaultDownloadDirectory
			};
		}

		private static string DetermineFileName(Enclosure enclosure)
		{
			return enclosure.Link.Segments.Last() switch
			{
				string { Length: > 0 } value => value,
				_ => $"rdr-unnamed-download-{DateTimeOffset.Now.Ticks}"
			};
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

			var allItems = from feed in rdrService.Feeds
						   from item in feed.Items
						   orderby item.Published descending
						   select item;

			return MoveItemsAsync(allItems, clearFirst: true);
		}

		private Task MoveUnreadItemsAsync(bool clearFirst)
		{
			var unreadItems = from f in rdrService.Feeds
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

		private static string DetermineFeedsFileFullPath(RdrOptions rdrOptions)
		{
			return rdrOptions.FeedsFilePath switch
			{
				string { Length: > 0 } value => Path.IsPathRooted(value)
					? File.Exists(value)
						? value
						: defaultFeedsFilePath
					: Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), value),
				_ => defaultFeedsFilePath
			};
		}

		public void Exit(Window window)
		{
			ArgumentNullException.ThrowIfNull(window);

			LogWindowExit(logger, window.Name);

			window.Close();
		}
	}

	internal static partial class MainWindowViewModelLoggerMessages
	{
		[LoggerMessage(GoToFeedLinkNullId, LogLevel.Warning, "feed link was null ({FeedName})")]
		internal static partial void LogGoToFeedLinkNull(ILogger<MainWindowViewModel> logger, string feedName);

		[LoggerMessage(GoToFeedFailedId, LogLevel.Warning, "feed link launch failed ({FeedName})")]
		internal static partial void LogGoToFeedFailed(ILogger<MainWindowViewModel> logger, string feedName);

		[LoggerMessage(GoToFeedId, LogLevel.Debug, "opened in browser ('{FeedName}')")]
		internal static partial void LogGoToFeed(ILogger<MainWindowViewModel> logger, string feedName);

		[LoggerMessage(GoToItemLinkNullId, LogLevel.Warning, "item link was null ({FeedName}: {ItemName})")]
		internal static partial void LogGoToItemLinkNull(ILogger<MainWindowViewModel> logger, string feedName, string itemName);

		[LoggerMessage(GoToItemFailedId, LogLevel.Warning, "failed to launch item URI: {Link}")]
		internal static partial void LogGoToItemFailed(ILogger<MainWindowViewModel> logger, string link);

		[LoggerMessage(GoToItemId, LogLevel.Debug, "opened in browser ('{ItemName}')")]
		internal static partial void LogGoToItem(ILogger<MainWindowViewModel> logger, string itemName);

		[LoggerMessage(FeedsFileOpenedId, LogLevel.Debug, "opened feeds file ('{Path}')")]
		internal static partial void LogFeedsFileOpened(ILogger<MainWindowViewModel> logger, string path);

		[LoggerMessage(FeedsFileErrorId, LogLevel.Error, "feeds file path does not exist ({Path}), or process launch failed")]
		internal static partial void LogFeedsFileError(ILogger<MainWindowViewModel> logger, string path);

		[LoggerMessage(ReloadFeedsFileStartedId, LogLevel.Debug, "started reloading feeds file ({Path})")]
		internal static partial void LogReloadFeedsFileStarted(ILogger<MainWindowViewModel> logger, string path);

		[LoggerMessage(FeedsFileEmptyId, LogLevel.Warning, "feeds file contains no feeds ({Path})")]
		internal static partial void LogFeedsFileEmpty(ILogger<MainWindowViewModel> logger, string path);

		[LoggerMessage(ReloadFeedsFileFinishedId, LogLevel.Debug, "reload feeds file finished ({Path})")]
		internal static partial void LogReloadFeedsFileFinished(ILogger<MainWindowViewModel> logger, string path);

		[LoggerMessage(FeedsFileDoesNotExistId, LogLevel.Error, "feeds file does not exist: missing '{MissingPath}', attempted '{AttemptedPath}'")]
		internal static partial void LogFeedsFileDoesNotExist(ILogger<MainWindowViewModel> logger, string missingPath, string attemptedPath);

		[LoggerMessage(DownloadLocalFilePathId, LogLevel.Trace, "will attempt to download '{Link}' to '{LocalFilePath}'")]
		internal static partial void LogDownloadLocalFilePath(ILogger<MainWindowViewModel> logger, string link, string localFilePath);

		[LoggerMessage(DownloadProgressId, LogLevel.Trace, "progress of '{Link}' to '{LocalPath}': {Message}")]
		internal static partial void LogDownloadProgress(ILogger<MainWindowViewModel> logger, string link, string localPath, string message);

		[LoggerMessage(DownloadStartedId, LogLevel.Debug, "started downloading '{Link}' to '{LocalPath}'")]
		internal static partial void LogDownloadStarted(ILogger<MainWindowViewModel> logger, string link, string localPath);

		[LoggerMessage(DownloadFinishedId, LogLevel.Information, "downloaded '{Link}' to '{LocalPath}'")]
		internal static partial void LogDownloadFinished(ILogger<MainWindowViewModel> logger, string link, string localPath);

		[LoggerMessage(DownloadFailedId, LogLevel.Error, "download failed: {Reason} - {StatusCode} for '{Link}'")]
		internal static partial void LogDownloadFailed(ILogger<MainWindowViewModel> logger, Reason reason, HttpStatusCode? statusCode, string link);

		[LoggerMessage(WindowExitId, LogLevel.Debug, "window exit ('{WindowName}')")]
		internal static partial void LogWindowExit(ILogger<MainWindowViewModel> logger, string windowName);
	}
}
