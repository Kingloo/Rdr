using System;
using System.Net;

namespace RdrLib.Helpers
{
	public static class HttpStatusCodeHelpers
	{
		public static string FormatStatusCode(HttpStatusCode? statusCode)
		{
			ArgumentNullException.ThrowIfNull(statusCode);

			return $"{(int)statusCode} {statusCode}";
		}

		public static bool IsInformational(HttpStatusCode? statusCode)
		{
			return IsWithin(statusCode, 100, 200);
		}

		public static bool IsSuccess(HttpStatusCode? statusCode)
		{
			return IsWithin(statusCode, 200, 300);
		}

		public static bool IsRedirection(HttpStatusCode? statusCode)
		{
			return IsWithin(statusCode, 300, 400);
		}

		public static bool IsClientError(HttpStatusCode? statusCode)
		{
			return IsWithin(statusCode, 400, 500);
		}

		public static bool IsServerError(HttpStatusCode? statusCode)
		{
			return IsWithin(statusCode, 500, 600);
		}

		private static bool IsWithin(HttpStatusCode? statusCode, uint lowerLimit, uint upperLimit)
		{
			// https://en.wikipedia.org/wiki/List_of_HTTP_status_codes

			// lowerLimit is inclusive
			// upperLimit is exclusive

			if (statusCode is null)
			{
				return false;
			}

			uint statusCodeValue = (uint)statusCode;

			return statusCodeValue >= lowerLimit && statusCodeValue < upperLimit;
		}
	}
}
