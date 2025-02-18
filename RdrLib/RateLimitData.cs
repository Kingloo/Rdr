using System;
using System.Net;

namespace RdrLib
{
	internal sealed class RateLimitData
	{
		internal HttpStatusCode StatusCode { get; init; } = HttpStatusCode.Unused;
		internal DateTimeOffset Timestamp { get; init; } = DateTimeOffset.MinValue;
		internal TimeSpan Backoff { get; set; } = TimeSpan.Zero;
		internal RateLimitIncreaseStrategy RateLimitIncreaseStrategy { get; set; } = RateLimitIncreaseStrategy.None;
		internal RateLimitLiftedStrategy RateLimitLiftedStrategy { get; set; } = RateLimitLiftedStrategy.None;

		internal RateLimitData(HttpStatusCode statusCode, DateTimeOffset timestamp)
		{
			if (timestamp < DateTimeOffset.Now.AddDays(-1d))
			{
				throw new ArgumentException("timestamp cannot be older than Now", nameof(timestamp));
			}

			StatusCode = statusCode;
			Timestamp = timestamp;
		}

		public override string ToString()
		{
			return $"{Timestamp} | {StatusCode},{Backoff},{RateLimitIncreaseStrategy},{RateLimitLiftedStrategy}";
		}
	}
}
