using Microsoft.Extensions.Logging;

namespace RdrLib
{
	internal static class EventIds
	{
		internal static class RdrService
		{
			internal static readonly EventId FeedUpdateStarted = new EventId(100, nameof(FeedUpdateStarted));
			internal static readonly EventId FeedUpdateSucceeded = new EventId(101, nameof(FeedUpdateSucceeded));
			internal static readonly EventId FeedUpdateFailed = new EventId(102, nameof(FeedUpdateFailed));
			internal static readonly EventId FeedAdded = new EventId(103, nameof(FeedAdded));
			internal static readonly EventId FeedRemoved = new EventId(104, nameof(FeedRemoved));
			internal static readonly EventId FeedRemovedFailed = new EventId(105, nameof(FeedRemovedFailed));
		}
	}
}