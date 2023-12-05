using Microsoft.Extensions.Logging;

namespace RdrLib.EventIds
{
	internal static class RdrService
	{
		internal const int FeedUpdateStartedId = 100;
		internal const int FeedUpdateSucceededId = 101;
		internal const int FeedUpdateFailedId = 102;
		internal const int FeedAddedId = 103;
		internal const int FeedRemovedId = 104;
		internal const int FeedRemovedFailedId = 105;
		internal const int MarkAsReadId = 106;
		internal const int MarkAllAsReadId = 107;
		internal const int ClearFeedsId = 108;

		internal static readonly EventId FeedUpdateStarted = new EventId(FeedUpdateStartedId, nameof(FeedUpdateStarted));
		internal static readonly EventId FeedUpdateSucceeded = new EventId(FeedUpdateSucceededId, nameof(FeedUpdateSucceeded));
		internal static readonly EventId FeedUpdateFailed = new EventId(FeedUpdateFailedId, nameof(FeedUpdateFailed));
		internal static readonly EventId FeedAdded = new EventId(FeedAddedId, nameof(FeedAdded));
		internal static readonly EventId FeedRemoved = new EventId(FeedRemovedId, nameof(FeedRemoved));
		internal static readonly EventId FeedRemovedFailed = new EventId(FeedRemovedFailedId, nameof(FeedRemovedFailed));
		internal static readonly EventId MarkAsRead = new EventId(MarkAsReadId, nameof(MarkAsRead));
		internal static readonly EventId MarkAllAsRead = new EventId(MarkAllAsReadId, nameof(MarkAllAsRead));
		internal static readonly EventId ClearFeeds = new EventId(ClearFeedsId, nameof(ClearFeeds));
	}
}
