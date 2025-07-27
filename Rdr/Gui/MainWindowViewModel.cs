using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Rdr.Common;
using RdrLib;
using RdrLib.Model;
using RdrLib.Services.Updater;
using static Rdr.EventIds.MainWindowViewModel;
using static Rdr.Gui.MainWindowViewModelLoggerMessages;
using static RdrLib.Helpers.HttpStatusCodeHelpers;

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

		private DelegateCommandAsync<Feed>? refreshForceCommand = null;
		public DelegateCommandAsync<Feed> RefreshForceCommand
		{
			get
			{
				refreshForceCommand ??= new DelegateCommandAsync<Feed>(RefreshForceAsync, CanExecuteAsync);

				return refreshForceCommand;
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

		private DelegateCommandAsync<int>? seeRecentCommand = null;
		public DelegateCommandAsync<int> SeeRecentCommand
		{
			get
			{
				seeRecentCommand ??= new DelegateCommandAsync<int>(SeeRecentAsync, (_) => !Activity);

				return seeRecentCommand;
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
				Enclosure enclosure => enclosure.IsDownloading == false,
				_ => Activity == false
			};
		}

		private bool activity = false;
		public bool Activity
		{
			get => activity;
			set
			{
				SetProperty(ref activity, value, nameof(Activity));

				RaiseCanExecuteChangedForAsyncCommands();
			}
		}

		private void RaiseCanExecuteChangedForAsyncCommands()
		{
			RefreshAllCommand.RaiseCanExecuteChanged();
			RefreshCommand.RaiseCanExecuteChanged();
			ReloadCommand.RaiseCanExecuteChanged();
			SeeUnreadCommand.RaiseCanExecuteChanged();
			SeeRecentCommand.RaiseCanExecuteChanged();
			SeeAllCommand.RaiseCanExecuteChanged();
			ViewFeedItemsCommand.RaiseCanExecuteChanged();
		}

		private string statusMessage = "no status message";
		public string StatusMessage
		{
			get => statusMessage;
			set => SetProperty(ref statusMessage, value, nameof(StatusMessage));
		}

		private readonly ObservableCollection<Item> viewedItems = new ObservableCollection<Item>();
		public IReadOnlyCollection<Item> ViewedItems { get => viewedItems; }

		private readonly ObservableCollection<Feed> feeds = new ObservableCollection<Feed>();
		public IReadOnlyCollection<Feed> Feeds { get => feeds; }

		private readonly IRdrService rdrService;
		private readonly IOptionsMonitor<RdrOptions> rdrOptionsMonitor;
		private readonly ILogger<MainWindowViewModel> logger;

		private Feed? selectedFeed = null;
		private ViewMode _viewMode = ViewMode.Empty;
		private DispatcherTimer? refreshTimer = null;

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

			_viewMode = ViewMode.Unread;
		}

		public void StartRefreshTimer()
		{
			if (refreshTimer is null)
			{
				refreshTimer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
				{
					Interval = rdrOptionsMonitor.CurrentValue.UpdateInterval
				};

				refreshTimer.Tick += RefreshTimer_Tick;

				refreshTimer.Start();
			}
		}

		public void StopRefreshTimer()
		{
			if (refreshTimer is not null)
			{
				refreshTimer.Stop();
				refreshTimer.Tick -= RefreshTimer_Tick;
				refreshTimer = null;
			}
		}

		private void RefreshTimer_Tick(object? _, EventArgs __)
		{
			RefreshAllCommand.Execute();
		}

		private Task RefreshAllAsync()
			=> RefreshAsync(Feeds.ToList());

		private async Task RefreshAsync(List<Feed> feeds)
		{
			Activity = true;

			StatusMessage = "updating ...";

			IReadOnlyList<FeedUpdateContext> contexts;

			using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMinutes(3d)))
			{
				try
				{
					contexts = await rdrService.UpdateAsync(
						feeds,
						rdrOptionsMonitor.CurrentValue,
						beConditional: true,
						cts.Token)
					.ConfigureAwait(true);
				}
				catch (OperationCanceledException)
				{
					contexts = new List<FeedUpdateContext>(capacity: 0).AsReadOnly();
				}
			}

			LogRateLimits(contexts);
			LogFeedStatusOther(contexts);

			switch (_viewMode)
			{
				case ViewMode.Feed:
					if (selectedFeed is not null)
					{
						await MoveItemsAsync(selectedFeed).ConfigureAwait(true);
					}
					break;
				case ViewMode.Recent:
				case ViewMode.Unread:
					await MoveUnreadItemsAsync(clearFirst: false).ConfigureAwait(true);
					break;
				default:
					break;
			}

			ShowLastUpdatedMessage();

			Activity = false;
		}

		private Task RefreshAsync(Feed feed)
			=> RefreshAsync(feed, force: false);

		private Task RefreshForceAsync(Feed feed)
			=> RefreshAsync(feed, force: true);

		private async Task RefreshAsync(Feed feed, bool force)
		{
			Activity = rdrService.IsUpdating;

			FeedUpdateContext context = await rdrService.UpdateAsync(
				feed,
				rdrOptionsMonitor.CurrentValue,
				beConditional: !force,
				CancellationToken.None)
			.ConfigureAwait(true);

			LogRateLimit(context);
			LogFeedStatusOther(context);

			if (_viewMode == ViewMode.Feed
				&& selectedFeed is not null
				&& selectedFeed == feed)
			{
				await MoveItemsAsync(feed).ConfigureAwait(true);
			}

			Activity = rdrService.IsUpdating;
		}

		private void LogRateLimit(FeedUpdateContext context)
		{
			if (context.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
			{
				DateTimeOffset now = DateTimeOffset.Now;
				DateTimeOffset rateLimitExpiration = context.Finish + context.RateLimit;

				TimeSpan rateLimitRemaining = rateLimitExpiration > now
					? rateLimitExpiration - now
					: TimeSpan.Zero;

				LogFeedRateLimited(logger, context.Uri.AbsoluteUri, FormatTimeSpan(context.RateLimit), FormatTimeSpan(rateLimitRemaining));
			}
		}

		private void LogRateLimits(IReadOnlyList<FeedUpdateContext> contexts)
		{
			foreach (FeedUpdateContext each in contexts)
			{
				LogRateLimit(each);
			}
		}

		private void LogFeedStatusOther(FeedUpdateContext context)
			=> LogFeedStatusOther(new List<FeedUpdateContext>(capacity: 1) { context }.AsReadOnly());

		private void LogFeedStatusOther(IReadOnlyList<FeedUpdateContext> contexts)
		{
			var nameAndStatusCodeOfFeedsWithStatusOther = Feeds
				.Where(feed => feed.Status == FeedStatus.Other)
				.Join(
					contexts,
					feed => feed.Link,
					context => context.Uri,
					(feed, context) => new
					{
						Name = feed.Name,
						StatusCode = context.StatusCode
					}
				);

			foreach (var each in nameAndStatusCodeOfFeedsWithStatusOther)
			{
				MainWindowViewModelLoggerMessages.LogFeedStatusOther(logger, each.Name, FormatStatusCode(each.StatusCode));
			}
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

			MarkAsRead(item);

			if (_viewMode == ViewMode.Unread)
			{
				viewedItems.Remove(item);
			}

			LogGoToItem(logger, item.FeedName, item.Name);
		}

		public void MarkAsRead(Item item)
		{
			ArgumentNullException.ThrowIfNull(item);

			item.Unread = false;
		}

		public void MarkAsRead(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			foreach (Item each in feed.Items)
			{
				MarkAsRead(each);
			}
		}

		private void MarkAllAsRead()
		{
			switch (_viewMode)
			{
				case ViewMode.Feed:
				case ViewMode.Recent:
					{
						foreach (Item each in viewedItems)
						{
							MarkAsRead(each);
						}
					}
					break;
				case ViewMode.Unread:
					{
						foreach (Feed feed in Feeds)
						{
							foreach (Item item in feed.Items)
							{
								MarkAsRead(item);
							}
						}

						viewedItems.Clear();
					}
					break;
				default:
					break;
			}
		}

		private void OpenFeedsFile()
		{
			FileInfo feedsFile = new FileInfo(rdrOptionsMonitor.CurrentValue.FeedsFilePath);

			if (SystemLaunch.Path(feedsFile.FullName))
			{
				LogFeedsFileOpened(logger, feedsFile.FullName);
			}
			else
			{
				LogFeedsFileError(logger, feedsFile.FullName);
			}
		}

		private async Task ReloadAsync()
		{
			FileInfo file = new FileInfo(rdrOptionsMonitor.CurrentValue.FeedsFilePath);

			IReadOnlyList<Feed> loadedFeeds;

			using (
				FileStream fileStream = new FileStream(
					file.FullName,
					FileMode.Open,
					FileAccess.Read,
					FileShare.Read,
					bufferSize: 4096,
					FileOptions.Asynchronous | FileOptions.SequentialScan)
				)
			{
				loadedFeeds = await rdrService.LoadAsync(fileStream, CancellationToken.None).ConfigureAwait(true);
			}

			RemoveOldFeeds(loadedFeeds);

			List<Feed> feedsToRefresh = await AddNewFeeds(loadedFeeds).ConfigureAwait(true);

			if (Feeds.Count > 0)
			{
				await RefreshAsync(feedsToRefresh).ConfigureAwait(true);
			}
		}

		private void RemoveOldFeeds(IReadOnlyList<Feed> loadedFeeds)
		{
			if (loadedFeeds.Count == 0)
			{
				feeds.Clear();
			}

			List<Feed> toRemove = Feeds.Where(feed => loadedFeeds.Contains(feed) == false).ToList();

			foreach (Feed each in toRemove)
			{
				feeds.Remove(each);
			}
		}

		private async Task<List<Feed>> AddNewFeeds(IReadOnlyList<Feed> loadedFeeds)
		{
			List<Feed> toRefresh = new List<Feed>(capacity: loadedFeeds.Count);

			foreach (Feed[] chunk in loadedFeeds.Chunk(5))
			{
				foreach (Feed each in chunk)
				{
					if (feeds.Contains(each) == false)
					{
						feeds.Add(each);
						toRefresh.Add(each);
					}
				}

				await Dispatcher.Yield(DispatcherPriority.Background);
			}

			return toRefresh;
		}

		private async Task DownloadEnclosureAsync(Enclosure enclosure)
		{
			ArgumentNullException.ThrowIfNull(enclosure);

			DirectoryInfo downloadDirectory = new DirectoryInfo(rdrOptionsMonitor.CurrentValue.DownloadDirectory);

			if (!downloadDirectory.Exists)
			{
				return;
			}

			string filename = DetermineFileName(enclosure);

			string fullPath = Path.Combine(downloadDirectory.FullName, filename);

			FileInfo file = new FileInfo(fullPath);

			if (file.Exists)
			{
				LogFileExists(logger, file.FullName);

				return;
			}

			LogDownloadLocalFilePath(logger, enclosure.Link.AbsoluteUri, fullPath);

			Progress<FileDownloadProgress> progress = new Progress<FileDownloadProgress>((FileDownloadProgress e) =>
			{
				string message = CreateEnclosureMessage(e);

				enclosure.Message = message;

				LogDownloadProgress(logger, enclosure.Link.AbsoluteUri, fullPath, message);
			});

			enclosure.IsDownloading = true;

			LogDownloadStarted(logger, enclosure.FeedName, enclosure.Link.AbsoluteUri, fullPath);

			try
			{
				long bytesDownloaded = await rdrService.DownloadEnclosureAsync(enclosure, file, progress, CancellationToken.None).ConfigureAwait(true);

				LogDownloadFinished(logger, enclosure.FeedName, enclosure.Link.AbsoluteUri, file.FullName);

				enclosure.Message = "Download";
			}
			catch (HttpIOException ex)
			{
				enclosure.Message = $"{ex.HttpRequestError}";

				LogDownloadFailed(logger, ex.HttpRequestError.ToString(), enclosure.FeedName, enclosure.Link.AbsoluteUri);
			}
			catch (HttpRequestException ex)
			{
				enclosure.Message = $"{ex.StatusCode}";

				LogDownloadFailed(logger, ex.HttpRequestError.ToString(), enclosure.FeedName, enclosure.Link.AbsoluteUri);
			}
			catch (IOException ex)
			{
				enclosure.Message = "i/o error";

				LogDownloadFailed(logger, ex.Message, enclosure.FeedName, enclosure.Link.AbsoluteUri);
			}

			enclosure.IsDownloading = false;
		}

		private static string DetermineFileName(Enclosure enclosure)
		{
			return enclosure.Link.Segments.Last() switch
			{
				string { Length: > 0 } value => value,
				_ => $"rdr-unnamed-download-{DateTimeOffset.Now.Ticks}"
			};
		}

		private static string CreateEnclosureMessage(FileDownloadProgress e)
		{
			CultureInfo cc = CultureInfo.CurrentCulture;

			if (e.ContentLength.HasValue)
			{
				decimal ratio = Convert.ToDecimal(e.TotalBytesWritten) / Convert.ToDecimal(e.ContentLength.Value);
				string format = string.Format(cc, "0{0}00 {1}", cc.NumberFormat.PercentDecimalSeparator, cc.NumberFormat.PercentSymbol);

				return ratio.ToString(format, cc);
			}
			else
			{
				return $"{e.TotalBytesWritten} bytes written";
			}
		}

		private async Task ViewFeedItemsAsync(Feed? feed)
		{
			if (feed is null)
			{
				SetViewMode(ViewMode.Unread);

				await MoveUnreadItemsAsync(clearFirst: true).ConfigureAwait(true);
			}
			else
			{
				SetViewMode(ViewMode.Feed, feed);

				await MoveItemsAsync(feed.Items, clearFirst: true).ConfigureAwait(true);
			}
		}

		private Task SeeUnreadAsync()
		{
			SetViewMode(ViewMode.Unread);

			return MoveUnreadItemsAsync(clearFirst: true);
		}

		private Task SeeRecentAsync(int recentAmount) => SeeSomeAsync(recentAmount);

		private Task SeeAllAsync() => SeeSomeAsync(Int32.MaxValue);

		private Task SeeSomeAsync(int count)
		{
			SetViewMode(ViewMode.Recent);

			IEnumerable<Item> countOfItems = Feeds
				.SelectMany(static feed => feed.Items)
				.OrderByDescending(static item => item.Published)
				.Take(count);

			return MoveItemsAsync(countOfItems, clearFirst: true);
		}

		private Task MoveUnreadItemsAsync(bool clearFirst)
		{
			IEnumerable<Item> unreadItems = Feeds.SelectMany(static feed => feed.Items).Where(static item => item.Unread);

			return MoveItemsAsync(unreadItems, clearFirst);
		}

		private Task MoveItemsAsync(Feed feed)
			=> MoveItemsAsync(feed.Items, clearFirst: false);

		private async Task MoveItemsAsync(IEnumerable<Item> items, bool clearFirst)
		{
			Activity = true;

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

			Activity = false;
		}

		private void SetViewMode(ViewMode viewMode)
			=> SetViewMode(viewMode, null);

		private void SetViewMode(ViewMode viewMode, Feed? feed)
		{
			switch (viewMode)
			{
				case ViewMode.Empty:
				case ViewMode.Recent:
				case ViewMode.Unread:
					selectedFeed = null;
					break;
				case ViewMode.Feed:
					selectedFeed = feed ?? throw new ArgumentNullException(nameof(feed), "tried to set ViewMode to Feed with a feed that was null");
					break;
				default:
					break;
			}

			_viewMode = viewMode;
		}

		private void ShowLastUpdatedMessage()
		{
			string time = DateTimeOffset.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.CurrentCulture);

			StatusMessage = string.Format(CultureInfo.CurrentCulture, "last updated at {0}", time);
		}

		private string FormatTimeSpan(TimeSpan rateLimit)
		{
			const string timeSpanFormat = @"hh\:mm\:ss";
			const string timeSpanFormatWithDays = @"d\.hh\:mm\:ss";

			return rateLimit switch
			{
				{ Ticks: >= TimeSpan.TicksPerDay } => rateLimit.ToString(timeSpanFormatWithDays, CultureInfo.CurrentCulture),
				{ Ticks: 0 } => "zero",
				{ Ticks: < 0 } => throw new ArgumentException("rate limit cannot be negative", nameof(rateLimit)),
				_ => rateLimit.ToString(timeSpanFormat, CultureInfo.CurrentCulture)
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

		[LoggerMessage(FeedStatusOtherId, LogLevel.Warning, "{StatusCode} for feed '{FeedName}'")]
		internal static partial void LogFeedStatusOther(ILogger<MainWindowViewModel> logger, string feedName, string statusCode);

		[LoggerMessage(GoToFeedFailedId, LogLevel.Warning, "feed link launch failed ({FeedName})")]
		internal static partial void LogGoToFeedFailed(ILogger<MainWindowViewModel> logger, string feedName);

		[LoggerMessage(GoToFeedId, LogLevel.Debug, "opened in browser ('{FeedName}')")]
		internal static partial void LogGoToFeed(ILogger<MainWindowViewModel> logger, string feedName);

		[LoggerMessage(GoToItemLinkNullId, LogLevel.Warning, "item link was null ({FeedName}: {ItemName})")]
		internal static partial void LogGoToItemLinkNull(ILogger<MainWindowViewModel> logger, string feedName, string itemName);

		[LoggerMessage(GoToItemFailedId, LogLevel.Warning, "failed to launch item URI: {Link}")]
		internal static partial void LogGoToItemFailed(ILogger<MainWindowViewModel> logger, string link);

		[LoggerMessage(GoToItemId, LogLevel.Debug, "opened in browser ('{FeedName}'->'{ItemName}')")]
		internal static partial void LogGoToItem(ILogger<MainWindowViewModel> logger, string feedName, string itemName);

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

		[LoggerMessage(DownloadStartedId, LogLevel.Debug, "started downloading for '{FeedName}' from '{Link}' to '{LocalPath}'")]
		internal static partial void LogDownloadStarted(ILogger<MainWindowViewModel> logger, string feedName, string link, string localPath);

		[LoggerMessage(DownloadFinishedId, LogLevel.Information, "downloaded '{FeedName}' from '{Link}' to '{LocalPath}'")]
		internal static partial void LogDownloadFinished(ILogger<MainWindowViewModel> logger, string feedName, string link, string localPath);

		[LoggerMessage(DownloadFinishedDifferentPathId, LogLevel.Warning, "downloaded '{FeedName}' from '{Link}' to '{DifferentLocalPath}'")]
		internal static partial void LogDownloadFinishedDifferentPath(ILogger<MainWindowViewModel> logger, string feedName, string link, string differentlocalPath);

		[LoggerMessage(DownloadFailedId, LogLevel.Error, "download failed: '{Reason}' - for '{FeedName}' from '{Link}'")]
		internal static partial void LogDownloadFailed(ILogger<MainWindowViewModel> logger, string reason, string feedName, string link);

		[LoggerMessage(FileExistsId, LogLevel.Error, "file already exists - {FullPath}'")]
		internal static partial void LogFileExists(ILogger<MainWindowViewModel> logger, string fullPath);

		[LoggerMessage(FeedRateLimitedId, LogLevel.Warning, "rate limited for '{Uri}' for {RateLimit}, {RateLimitRemaining} remaining")]
		internal static partial void LogFeedRateLimited(ILogger<MainWindowViewModel> logger, string Uri, string RateLimit, string RateLimitRemaining);

		[LoggerMessage(WindowExitId, LogLevel.Debug, "window exit ('{WindowName}')")]
		internal static partial void LogWindowExit(ILogger<MainWindowViewModel> logger, string windowName);
	}
}
