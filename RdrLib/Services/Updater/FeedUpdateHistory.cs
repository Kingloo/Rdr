using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using RdrLib.Model;

namespace RdrLib.Services.Updater
{
	public class FeedUpdateHistory
	{
		private readonly ConcurrentDictionary<Uri, FeedUpdateContext> items = new ConcurrentDictionary<Uri, FeedUpdateContext>();

		public FeedUpdateHistory() { }

		public bool Update(Feed feed, FeedUpdateContext newContext)
		{
			ArgumentNullException.ThrowIfNull(feed);
			ArgumentNullException.ThrowIfNull(newContext);

			if (items.TryGetValue(feed.Link, out FeedUpdateContext? existingContext))
			{
				existingContext.Start = newContext.Start;
				existingContext.Finish = newContext.Finish;
				existingContext.StatusCode = newContext.StatusCode;
				existingContext.Exception = newContext.Exception;

				UpdateLastModified(existingContext, newContext);
				UpdateETag(existingContext, newContext);
				UpdateRateLimit(existingContext, newContext);

				return true;
			}
			else
			{
				return items.TryAdd(feed.Link, newContext);
			}
		}

		private void UpdateLastModified(FeedUpdateContext existingContext, FeedUpdateContext newContext)
		{
			if (newContext.LastModified is DateTimeOffset newLastModified
				&& newLastModified > existingContext.LastModified)
			{
				existingContext.LastModified = newLastModified;
			}
		}

		private void UpdateETag(FeedUpdateContext existingContext, FeedUpdateContext newContext)
		{
			if (newContext.ETag is EntityTagHeaderValue newETag && newETag != existingContext.ETag)
			{
				existingContext.ETag = newETag;
			}
		}

		private void UpdateRateLimit(FeedUpdateContext existingContext, FeedUpdateContext newContext)
		{
			// we only want to learn a new minimum rate limit from an HTTP 429 Too Many Requests
			// and not from a TimeoutException

			if (newContext.StatusCode == System.Net.HttpStatusCode.TooManyRequests
				&& newContext.RateLimit > existingContext.RateLimit)
			{
				existingContext.RateLimit = newContext.RateLimit;
			}
		}

		public bool Remove(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			return items.Remove(feed.Link, out var _);
		}

		public void ClearAll()
		{
			items.Clear();
		}

		public void Clear(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			if (items.TryGetValue(feed.Link, out FeedUpdateContext? context))
			{
				context = new FeedUpdateContext(feed);
			}
		}

		public FeedUpdateContext? GetForFeed(Feed feed)
		{
			ArgumentNullException.ThrowIfNull(feed);

			return items.TryGetValue(feed.Link, out FeedUpdateContext? context)
				? context
				: null;
		}
	}
}
