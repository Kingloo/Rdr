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
		internal static readonly EventId FeedsFileOpened = new EventId(210, nameof(FeedsFileOpened));
		internal static readonly EventId FeedsFileError = new EventId(211, nameof(FeedsFileError));
		internal static readonly EventId GoToFeed = new EventId(220, nameof(GoToFeed));
		internal static readonly EventId GoToFeedFailed = new EventId(221, nameof(GoToFeedFailed));
		internal static readonly EventId GoToFeedLinkNull = new EventId(222, nameof(GoToFeedLinkNull));
		internal static readonly EventId GoToItem = new EventId(223, nameof(GoToItem));
		internal static readonly EventId GoToItemFailed = new EventId(224, nameof(GoToItemFailed));
		internal static readonly EventId GoToItemLinkNull = new EventId(225, nameof(GoToItemLinkNull));
		internal static readonly EventId ReloadFeedsFileStarted = new EventId(230, nameof(ReloadFeedsFileStarted));
		internal static readonly EventId FeedsFileEmpty = new EventId(231, nameof(FeedsFileEmpty));
		internal static readonly EventId ReloadFeedsFileFinished = new EventId(232, nameof(ReloadFeedsFileFinished));
		internal static readonly EventId DownloadStarted = new EventId(240, nameof(DownloadStarted));
		internal static readonly EventId DownloadFinished = new EventId(241, nameof(DownloadFinished));
		internal static readonly EventId DownloadFailed = new EventId(242, nameof(DownloadFailed));
		internal static readonly EventId DownloadProgress = new EventId(243, nameof(DownloadProgress));
		internal static readonly EventId DownloadLocalFilePath = new EventId(244, nameof(DownloadLocalFilePath));
	}
}