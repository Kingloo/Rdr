using Microsoft.Extensions.Logging;

namespace Rdr.EventIds
{
	internal static class App
	{
		internal const int StartupStartedId = 100;
		internal const int StartupFinishedId = 101;
		internal const int DispatcherUnhandledExceptionId = 102;
		internal const int DispatcherUnhandledExceptionEmptyId = 103;
		internal const int ExitedId = 104;
		internal const int ExitedNotZeroId = 105;

		internal static readonly EventId StartupStarted = new EventId(StartupStartedId, nameof(StartupStarted));
		internal static readonly EventId StartupFinished = new EventId(StartupFinishedId, nameof(StartupFinished));
		internal static readonly EventId DispatcherUnhandledException = new EventId(DispatcherUnhandledExceptionId, nameof(DispatcherUnhandledException));
		internal static readonly EventId DispatcherUnhandledExceptionEmpty = new EventId(DispatcherUnhandledExceptionEmptyId, nameof(DispatcherUnhandledExceptionEmpty));
		internal static readonly EventId Exited = new EventId(ExitedId, nameof(Exited));
		internal static readonly EventId ExitedNotZero = new EventId(ExitedNotZeroId, nameof(ExitedNotZero));
	}

	internal static class MainWindowViewModel
	{
		internal const int FeedsFileOpenedId = 210;
		internal const int FeedsFileErrorId = 211;
		internal const int GoToFeedId = 220;
		internal const int GoToFeedFailedId = 221;
		internal const int GoToFeedLinkNullId = 222;
		internal const int GoToItemId = 223;
		internal const int GoToItemFailedId = 224;
		internal const int GoToItemLinkNullId = 225;
		internal const int ReloadFeedsFileStartedId = 230;
		internal const int FeedsFileEmptyId = 231;
		internal const int ReloadFeedsFileFinishedId = 232;
		internal const int FeedsFileDoesNotExistId = 233;
		internal const int DownloadStartedId = 240;
		internal const int DownloadFinishedId = 241;
		internal const int DownloadFinishedDifferentPathId = 242;
		internal const int DownloadFailedId = 243;
		internal const int DownloadLocalFilePathId = 244;
		internal const int DownloadProgressId = 245;
		internal const int FileExistsId = 246;
		internal const int FeedRateLimitedId = 247;
		internal const int WindowExitId = 250;

		internal static readonly EventId FeedsFileOpened = new EventId(FeedsFileOpenedId, nameof(FeedsFileOpened));
		internal static readonly EventId FeedsFileError = new EventId(FeedsFileErrorId, nameof(FeedsFileError));
		internal static readonly EventId GoToFeed = new EventId(GoToFeedId, nameof(GoToFeed));
		internal static readonly EventId GoToFeedFailed = new EventId(GoToFeedFailedId, nameof(GoToFeedFailed));
		internal static readonly EventId GoToFeedLinkNull = new EventId(GoToFeedLinkNullId, nameof(GoToFeedLinkNull));
		internal static readonly EventId GoToItem = new EventId(GoToItemId, nameof(GoToItem));
		internal static readonly EventId GoToItemFailed = new EventId(GoToItemFailedId, nameof(GoToItemFailed));
		internal static readonly EventId GoToItemLinkNull = new EventId(GoToItemLinkNullId, nameof(GoToItemLinkNull));
		internal static readonly EventId ReloadFeedsFileStarted = new EventId(ReloadFeedsFileStartedId, nameof(ReloadFeedsFileStarted));
		internal static readonly EventId FeedsFileEmpty = new EventId(FeedsFileEmptyId, nameof(FeedsFileEmpty));
		internal static readonly EventId FeedsFileDoesNotExist = new EventId(FeedsFileDoesNotExistId, nameof(FeedsFileDoesNotExist));
		internal static readonly EventId ReloadFeedsFileFinished = new EventId(ReloadFeedsFileFinishedId, nameof(ReloadFeedsFileFinished));
		internal static readonly EventId DownloadStarted = new EventId(DownloadStartedId, nameof(DownloadStarted));
		internal static readonly EventId DownloadFinished = new EventId(DownloadFinishedId, nameof(DownloadFinished));
		internal static readonly EventId DownloadFailed = new EventId(DownloadFailedId, nameof(DownloadFailed));
		internal static readonly EventId DownloadProgress = new EventId(DownloadProgressId, nameof(DownloadProgress));
		internal static readonly EventId DownloadLocalFilePath = new EventId(DownloadLocalFilePathId, nameof(DownloadLocalFilePath));
		internal static readonly EventId FileExists = new EventId(FileExistsId, nameof(FileExists));
		internal static readonly EventId FeedRateLimited = new EventId(FeedRateLimitedId, nameof(FeedRateLimited));
		internal static readonly EventId WindowExit = new EventId(WindowExitId, nameof(WindowExit));
	}
}
