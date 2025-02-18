using System;
using System.Collections.Generic;
using System.Net;
using RdrLib.Exceptions;

namespace RdrLib
{
	internal sealed class RateLimitManager
	{
		private readonly Dictionary<Uri, RateLimitData> statusCodes = new Dictionary<Uri, RateLimitData>();

		internal RateLimitManager() { }

		internal bool ShouldPerformRequest(Uri uri)
		{
			ArgumentNullException.ThrowIfNull(uri);

			if (!statusCodes.TryGetValue(uri, out RateLimitData? rateLimitData))
			{
				return true;
			}

			if (rateLimitData.StatusCode == HttpStatusCode.TooManyRequests)
			{
				return HasTimeoutExpired(rateLimitData);
			}

			if (rateLimitData.StatusCode == HttpStatusCode.OK)
			{
				return rateLimitData.RateLimitLiftedStrategy switch
				{
					RateLimitLiftedStrategy.Maintain => HasTimeoutExpired(rateLimitData),
					RateLimitLiftedStrategy.Reset => true,
					_ => throw new RateLimitException("invalid rate limit lift strategy")
				};
			}

			return true;
		}

		internal void AddResponse(
			Uri uri,
			IResponse response,
			TimeSpan startingBackoffInterval,
			RateLimitIncreaseStrategy rateLimitIncreaseStrategy,
			RateLimitLiftedStrategy rateLimitLiftedStrategy)
		{
			ArgumentNullException.ThrowIfNull(uri);
			ArgumentNullException.ThrowIfNull(response);

			if (startingBackoffInterval < TimeSpan.Zero)
			{
				throw new ArgumentException("backoff interval cannot be less than zero", nameof(startingBackoffInterval));
			}

			RateLimitData newRateLimitData = new RateLimitData(
				statusCode: response.StatusCode ?? HttpStatusCode.Unused,
				timestamp: DateTimeOffset.Now
			)
			{
				Backoff = startingBackoffInterval,
				RateLimitIncreaseStrategy = rateLimitIncreaseStrategy,
				RateLimitLiftedStrategy = rateLimitLiftedStrategy
			};

			if (statusCodes.TryGetValue(uri, out RateLimitData? previousRateLimitData))
			{
				newRateLimitData.Backoff = response.StatusCode switch
				{
					HttpStatusCode.TooManyRequests => GetNewBackoff(previousRateLimitData.Backoff, rateLimitIncreaseStrategy),
					_ => rateLimitLiftedStrategy switch
					{
						RateLimitLiftedStrategy.Maintain => previousRateLimitData.Backoff,
						RateLimitLiftedStrategy.Reset => startingBackoffInterval,
						_ => throw new ArgumentException("invalid lifted strategy", nameof(rateLimitIncreaseStrategy))
					}
				};

				statusCodes[uri] = newRateLimitData;
			}
			else
			{
				newRateLimitData.Backoff = startingBackoffInterval;

				statusCodes.Add(uri, newRateLimitData);
			}
		}

		private TimeSpan GetNewBackoff(TimeSpan existingBackoff, RateLimitIncreaseStrategy rateLimitIncreaseStrategy)
		{
			TimeSpan max = TimeSpan.FromHours(36d);

			TimeSpan newBackoff = rateLimitIncreaseStrategy switch
			{
				RateLimitIncreaseStrategy.Double => existingBackoff * 2,
				RateLimitIncreaseStrategy.AddHour => existingBackoff.Add(TimeSpan.FromHours(1d)),
				RateLimitIncreaseStrategy.AddDay => existingBackoff.Add(TimeSpan.FromDays(1d)),
				_ => existingBackoff
			};

			return newBackoff > max
				? max
				: newBackoff;
		}

		private static bool HasTimeoutExpired(RateLimitData rateLimitData)
		{
			return DateTimeOffset.Now - rateLimitData.Timestamp > rateLimitData.Backoff;
		}
	}
}
