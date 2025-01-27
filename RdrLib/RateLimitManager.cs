using System;
using System.Collections.Generic;
using System.Net;

namespace RdrLib
{
	internal class RateLimitManager
	{
		private readonly Dictionary<Uri, RateLimitData> statusCodes = new Dictionary<Uri, RateLimitData>();

		internal RateLimitManager() { }

		internal bool ShouldPerformRequest(Uri uri, HttpStatusCode statusCode, TimeSpan interval)
		{
			ArgumentNullException.ThrowIfNull(uri);

			return statusCodes.TryGetValue(uri, out RateLimitData? rateLimitData)
				? rateLimitData.StatusCode == statusCode && DateTimeOffset.Now - rateLimitData.Timestamp > interval
				: true;
		}

		internal void AddResponse(Uri uri, IResponse response)
		{
			ArgumentNullException.ThrowIfNull(uri);
			ArgumentNullException.ThrowIfNull(response);
			
			RateLimitData newRateLimitData = new RateLimitData(
				statusCode: response.StatusCode ?? HttpStatusCode.Unused,
				timestamp: DateTimeOffset.Now
			);
			
			if (statusCodes.TryGetValue(uri, out RateLimitData? _))
			{
				statusCodes[uri] = newRateLimitData;
			}
			else
			{
				statusCodes.Add(uri, newRateLimitData);
			}
		}

		internal void ClearHistory(Uri uri)
		{
			statusCodes.Remove(uri);
		}

		internal void ClearAllHistory()
		{
			statusCodes.Clear();
		}
	}
}
