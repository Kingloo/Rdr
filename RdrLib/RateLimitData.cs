using System;
using System.Net;

namespace RdrLib
{
	internal sealed class RateLimitData
	{
		internal HttpStatusCode StatusCode { get; init; }
		internal DateTimeOffset Timestamp { get; init; }

		internal RateLimitData(HttpStatusCode statusCode, DateTimeOffset timestamp)
		{
			StatusCode = statusCode;
			Timestamp = timestamp;
		}
	}
}
