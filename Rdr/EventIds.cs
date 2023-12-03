using Microsoft.Extensions.Logging;

namespace Rdr
{
	internal static class EventIds
	{
		internal static class App
		{
			internal static readonly EventId StartupStarted = new EventId(100, nameof(StartupStarted));
			internal static readonly EventId StartupFinished = new EventId(101, nameof(StartupStarted));
			internal static readonly EventId UnhandledException = new EventId(102, nameof(UnhandledException));
			internal static readonly EventId UnhandledExceptionEmpty = new EventId(103, nameof(UnhandledExceptionEmpty));
			internal static readonly EventId ExitedNotZero = new EventId(198, nameof(ExitedNotZero));
			internal static readonly EventId Exited = new EventId(199, nameof(Exited));
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
}