using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using RdrLib.Helpers;
using RdrLib.Model;

namespace RdrLib.Services.Updater
{
	public class FeedUpdater
	{
		private static readonly IReadOnlyList<FeedUpdateContext> emptyList = new List<FeedUpdateContext>(capacity: 0).AsReadOnly();

		private readonly IHttpClientFactory httpClientFactory;
		private readonly FeedUpdateHistory feedUpdateHistory;
		private int countUpdated = 0;
		private Total total = new Total(0);

		public bool IsUpdating { get; private set; } = false;
		public event EventHandler<FeedUpdatedEventArgs> FeedUpdated = delegate { };
		private void OnFeedUpdated(Count count, Total total) => FeedUpdated.Invoke(this, new FeedUpdatedEventArgs(count, total));

		public FeedUpdater(IHttpClientFactory httpClientFactory, FeedUpdateHistory feedUpdateHistory)
		{
			ArgumentNullException.ThrowIfNull(httpClientFactory);
			ArgumentNullException.ThrowIfNull(feedUpdateHistory);

			this.httpClientFactory = httpClientFactory;
			this.feedUpdateHistory = feedUpdateHistory;
		}

		public async Task<IReadOnlyList<FeedUpdateContext>> UpdateAsync(IList<Feed> feeds, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(feeds);
			ArgumentNullException.ThrowIfNull(rdrOptions);
			ArgumentOutOfRangeException.ThrowIfNegative(feeds.Count);

			total = new Total(feeds.Count);

			if (feeds.Count == 0)
			{
				return emptyList;
			}

			if (feeds.Count == 1)
			{
				return await UpdateSingleAsync(feeds[0], rdrOptions, beConditional, cancellationToken).ConfigureAwait(false);
			}

			IEnumerable<IGrouping<string, Feed>> feedGroups = feeds.GroupBy(static feed => feed.Link.DnsSafeHost);

			List<Task<IReadOnlyList<FeedUpdateContext>>> updateTasks = new List<Task<IReadOnlyList<FeedUpdateContext>>>(capacity: feeds.Count);

			foreach (IGrouping<string, Feed> group in feedGroups)
			{
				Task<IReadOnlyList<FeedUpdateContext>> groupTask = UpdateManyAsync(group.ToList(), rdrOptions, beConditional, cancellationToken);

				updateTasks.Add(groupTask);
			}

			List<FeedUpdateContext> contexts = new List<FeedUpdateContext>(capacity: feeds.Count);

			ParallelOptions parallelOptions = new ParallelOptions
			{
				CancellationToken = cancellationToken,
				MaxDegreeOfParallelism = rdrOptions.UpdateConcurrency
			};

			await Parallel.ForEachAsync(updateTasks, parallelOptions, async (Task<IReadOnlyList<FeedUpdateContext>> task, CancellationToken token) =>
			{
				IReadOnlyList<FeedUpdateContext> ret = await task.ConfigureAwait(false);

				if (ret.Count > 0)
				{
					foreach (FeedUpdateContext context in ret.Where(static each => each is not null))
					{
						contexts.Add(context);
					}
				}
			})
			.ConfigureAwait(false);

			Interlocked.Exchange(ref countUpdated, 0);

			return contexts.AsReadOnly();
		}

		private async Task<IReadOnlyList<FeedUpdateContext>> UpdateSingleAsync(Feed feed, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			IsUpdating = true;

			List<FeedUpdateContext> ret = new List<FeedUpdateContext>(capacity: 1);

			try
			{
				using HttpClient client = SelectHttpClient(feed, rdrOptions);

				FeedUpdateContext singleUpdateContext = await UpdateFeedAsync(client, feed, rdrOptions, beConditional, cancellationToken).ConfigureAwait(false);

				if (singleUpdateContext is not null)
				{
					ret.Add(singleUpdateContext);
				}
			}
			finally
			{
				IsUpdating = false;
			}

			return ret.AsReadOnly();
		}

		private async Task<IReadOnlyList<FeedUpdateContext>> UpdateManyAsync(List<Feed> feeds, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			if (feeds.Count == 0)
			{
				return emptyList;
			}

			IsUpdating = true;

			List<FeedUpdateContext> contexts = new List<FeedUpdateContext>(capacity: feeds.Count);

			try
			{
				using HttpClient client = SelectHttpClient(feeds[0], rdrOptions);

				for (int i = 0; i < feeds.Count; i++)
				{
					FeedUpdateContext context = await UpdateFeedAsync(client, feeds[i], rdrOptions, beConditional, cancellationToken).ConfigureAwait(false);

					if (context is not null)
					{
						contexts.Add(context);
					}

					int currentUpdated = Interlocked.Increment(ref countUpdated);

					OnFeedUpdated(new Count(currentUpdated), total);

					if (i + 1 < feeds.Count)
					{
						await Task.Delay(rdrOptions.BatchUpdateDelay, cancellationToken).ConfigureAwait(false);
					}
				}
			}
			finally
			{
				IsUpdating = false;
			}

			return contexts.AsReadOnly();
		}

		private async Task<FeedUpdateContext> UpdateFeedAsync(HttpClient client, Feed feed, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			FeedUpdateContext context = new FeedUpdateContext(feed)
			{
				Start = DateTimeOffset.Now
			};

			try
			{
				FeedUpdateContext? newContext = await UpdateFeedAsyncUncaught(client, feed, rdrOptions, beConditional, cancellationToken).ConfigureAwait(false);

				if (newContext is not null)
				{
					context = newContext;
					context.Exception = null;
				}
			}
			catch (TimeoutException ex)
			{
				context.Exception = ex;
				context.Finish = DateTimeOffset.Now;

				feed.Status = FeedStatus.Timeout;
			}
			catch (HttpIOException ex)
			{
				context.Exception = ex;
				context.Finish = DateTimeOffset.Now;

				feed.Status = FeedStatus.InternetError;
			}
			catch (HttpRequestException ex)
			{
				context.Exception = ex;
				context.Finish = DateTimeOffset.Now;

				if (ex.HttpRequestError == HttpRequestError.SecureConnectionError
					&& ex.InnerException is AuthenticationException ae)
				{
					feed.Status = FeedStatus.ConnectionError;
				}
				else
				{
					feed.Status = FeedStatus.InternetError;
				}
			}
			catch (Exception ex) when (
				ex is IOException
				|| ex is SocketException
				|| ex is OperationCanceledException
			)
			{
				context.Exception = ex;
				context.Finish = DateTimeOffset.Now;

				if (ex is OperationCanceledException oce && oce.InnerException is TimeoutException)
				{
					feed.Status = FeedStatus.Timeout;
				}
				else
				{
					feed.Status = FeedStatus.Broken;
				}
			}

			feedUpdateHistory.Update(feed, context);

			return context;
		}

		private async Task<FeedUpdateContext> UpdateFeedAsyncUncaught(HttpClient client, Feed feed, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			TimeSpan rateLimitPadding = TimeSpan.FromSeconds(10d);

			feed.Status = FeedStatus.Updating;

			DateTimeOffset start = DateTimeOffset.Now;
			string responseData = string.Empty;

			FeedUpdateContext context = feedUpdateHistory.GetForFeed(feed)
				?? new FeedUpdateContext(feed);

			if (beConditional && IsUnderLastModifiedCooldown(context, start))
			{
				feed.Status = FeedStatus.Ok;
			}
			else if (beConditional && IsRateLimited(context, start, rateLimitPadding))
			{
				feed.Status = FeedStatus.RateLimited;
			}
			else if (beConditional && IsChoosingToObeyOldRateLimit(context, start, rateLimitPadding))
			{
				feed.Status = FeedStatus.Ok;
			}
			else if (beConditional && IsRateLimitedByHttpTimeout(context, start, rdrOptions.RateLimitOnHttpTimeout))
			{
				feed.Status = FeedStatus.Timeout;
			}
			else
			{
				void makeRequestConditional(HttpRequestMessage requestMessage)
				{
					requestMessage.Method = HttpMethod.Get;

					if (context?.ETag is EntityTagHeaderValue etag)
					{
						requestMessage.Headers.IfNoneMatch.Add(etag);
					}

					if (context?.LastModified is DateTimeOffset lastModified)
					{
						requestMessage.Headers.IfModifiedSince = lastModified;
					}
				}

				context.Start = start;

				using ResponseSet responseSet = beConditional
					? await Web2.PerformHeaderRequest(client, feed.Link, makeRequestConditional, cancellationToken).ConfigureAwait(false)
					: await Web2.PerformHeaderRequest(client, feed.Link, cancellationToken).ConfigureAwait(false);

				if (responseSet.Responses.LastOrDefault() is ResponseSetItem lastResponseSetItem)
				{
					context.StatusCode = lastResponseSetItem.Response.StatusCode;
					context.ETag = lastResponseSetItem.Response.Headers.ETag;
					context.RateLimit = SetRateLimit(context.RateLimit, lastResponseSetItem.Response.Headers.RetryAfter, start);

					if (lastResponseSetItem.Uri != feed.Link)
					{
						context.RedirectData = new RedirectData(From: feed.Link, To: lastResponseSetItem.Uri);
					}

					switch (lastResponseSetItem.Response.StatusCode)
					{
						case HttpStatusCode.OK:
							{
								context.LastModified = lastResponseSetItem.Response.Content.Headers.LastModified;

								responseData = await Web2.PerformBodyRequestToString(lastResponseSetItem.Response, cancellationToken).ConfigureAwait(false);

								feed.Status = ParseFeed(feed, responseData)
									? FeedStatus.Ok
									: FeedStatus.ParseFailed;

								break;
							}
						case HttpStatusCode.NotModified:
							feed.Status = FeedStatus.Ok;
							break;
						case HttpStatusCode.TooManyRequests:
							feed.Status = FeedStatus.RateLimited;
							break;
						case HttpStatusCode.Forbidden:
							feed.Status = FeedStatus.Forbidden;
							break;
						case HttpStatusCode.NotFound:
							feed.Status = FeedStatus.DoesNotExist;
							break;
						case HttpStatusCode.InternalServerError:
							feed.Status = FeedStatus.InternetError;
							break;
						default:
							feed.Status = FeedStatus.Other;
							break;
					}
				}
				else
				{
					feed.Status = FeedStatus.Broken;

					context.StatusCode = null;
				}

				context.Finish = DateTimeOffset.Now;
			}

			return context;
		}

		private bool ParseFeed(Feed feed, string responseData)
		{
			if (!XmlHelpers.TryParse(responseData, out XDocument? document))
			{
				return false;
			}

			string feedName = FeedHelpers.GetName(document);

			if (!String.Equals(feed.Name, feedName, StringComparison.Ordinal))
			{
				feed.Name = feedName;
			}

			feed.AddMany(FeedHelpers.GetItems(document, feed.Name));

			return true;
		}

		private static bool IsUnderLastModifiedCooldown(FeedUpdateContext? context, DateTimeOffset now)
		{
			if (context is null)
			{
				return false;
			}

			if (context.LastModified is not DateTimeOffset lastModified)
			{
				return false;
			}

			TimeSpan timeSinceLastUpdated = now - context.Finish;
			TimeSpan timeSinceLastModified = context.Finish - lastModified;

			if (timeSinceLastModified > TimeSpan.FromDays(50d)
				&& timeSinceLastUpdated < TimeSpan.FromDays(3d))
			{
				return true;
			}

			if (timeSinceLastModified > TimeSpan.FromDays(30d)
				&& timeSinceLastUpdated < TimeSpan.FromDays(2d))
			{
				return true;
			}

			if (timeSinceLastModified > TimeSpan.FromDays(20d)
				&& timeSinceLastUpdated < TimeSpan.FromDays(1d))
			{
				return true;
			}

			if (timeSinceLastModified > TimeSpan.FromDays(8d)
				&& timeSinceLastUpdated < TimeSpan.FromHours(3d))
			{
				return true;
			}

			if (timeSinceLastModified > TimeSpan.FromDays(3d)
				&& timeSinceLastUpdated < TimeSpan.FromHours(1d))
			{
				return true;
			}

			return false;
		}

		private bool IsRateLimited(FeedUpdateContext? context, DateTimeOffset now, TimeSpan rateLimitPadding)
		{
			if (context is null)
			{
				return false;
			}

			return (context.Finish + context.RateLimit + rateLimitPadding) > now
				&& context.StatusCode == HttpStatusCode.TooManyRequests;
		}

		private bool IsChoosingToObeyOldRateLimit(FeedUpdateContext? context, DateTimeOffset now, TimeSpan rateLimitPadding)
		{
			if (context is null)
			{
				return false;
			}

			return (context.Finish + context.RateLimit + rateLimitPadding) > now
				&& context.StatusCode != HttpStatusCode.TooManyRequests;
		}

		private bool IsRateLimitedByHttpTimeout(FeedUpdateContext? context, DateTimeOffset now, TimeSpan httpTimeoutDelay)
		{
			if (context is null)
			{
				return false;
			}

			if (context.Exception is OperationCanceledException oce
				&& oce.InnerException is TimeoutException)
			{
				return (context.Finish + httpTimeoutDelay) > now;
			}

			return false;
		}

		private TimeSpan SetRateLimit(TimeSpan existingRateLimit, RetryConditionHeaderValue? retryAfter, DateTimeOffset start)
		{
			if (retryAfter is null)
			{
				return existingRateLimit; // retain old rate limit
			}

			TimeSpan newRateLimit = Web2.GetTimeLeftOnRateLimit(retryAfter, start);

			return newRateLimit > existingRateLimit
				? newRateLimit
				: existingRateLimit;
		}

		private HttpClient SelectHttpClient(Feed feed, RdrOptions rdrOptions)
		{
			string httpClientName = rdrOptions
				.SkipCrlCheckFor
				.Any(skipDomain => string.Equals(skipDomain, feed.Link.DnsSafeHost, StringComparison.OrdinalIgnoreCase))
					? Constants.NoCrlCheckHttpClientName
					: string.Empty;

			return httpClientFactory.CreateClient(httpClientName);
		}
	}
}
