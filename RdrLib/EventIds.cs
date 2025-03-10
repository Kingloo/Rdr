using Microsoft.Extensions.Logging;

namespace RdrLib.EventIds
{
	internal static class RdrService
	{
		internal const int FeedUpdateStartedId = 100;
		internal const int FeedUpdateSucceededId = 101;
		internal const int ETagMatchId = 102;
		internal const int FeedUpdateFailedId = 103;
		internal const int FeedAddedId = 104;
		internal const int FeedRemovedId = 105;
		internal const int FeedRemovedFailedId = 106;
		internal const int MarkAsReadId = 107;
		internal const int MarkAllAsReadId = 108;
		internal const int ClearFeedsId = 109;
		internal const int TimeoutId = 110;
		internal const int NewRateLimitId = 111;
		internal const int ExistingRateLimitId = 112;
		internal const int LastModifiedUnchangedId = 113;

		internal static readonly EventId FeedUpdateStarted = new EventId(FeedUpdateStartedId, nameof(FeedUpdateStarted));
		internal static readonly EventId FeedUpdateSucceeded = new EventId(FeedUpdateSucceededId, nameof(FeedUpdateSucceeded));
		internal static readonly EventId ETagMatch = new EventId(ETagMatchId, nameof(ETagMatch));
		internal static readonly EventId FeedUpdateFailed = new EventId(FeedUpdateFailedId, nameof(FeedUpdateFailed));
		internal static readonly EventId FeedAdded = new EventId(FeedAddedId, nameof(FeedAdded));
		internal static readonly EventId FeedRemoved = new EventId(FeedRemovedId, nameof(FeedRemoved));
		internal static readonly EventId FeedRemovedFailed = new EventId(FeedRemovedFailedId, nameof(FeedRemovedFailed));
		internal static readonly EventId MarkAsRead = new EventId(MarkAsReadId, nameof(MarkAsRead));
		internal static readonly EventId MarkAllAsRead = new EventId(MarkAllAsReadId, nameof(MarkAllAsRead));
		internal static readonly EventId ClearFeeds = new EventId(ClearFeedsId, nameof(ClearFeeds));
		internal static readonly EventId Timeout = new EventId(TimeoutId, nameof(Timeout));
		internal static readonly EventId NewRateLimit = new EventId(NewRateLimitId, nameof(NewRateLimit));
		internal static readonly EventId ExistingRateLimit = new EventId(ExistingRateLimitId, nameof(ExistingRateLimit));
		internal static readonly EventId LastModifiedUnchanged = new EventId(LastModifiedUnchangedId, nameof(LastModifiedUnchanged));
	}
}
