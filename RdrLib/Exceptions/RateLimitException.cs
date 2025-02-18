using System;

namespace RdrLib.Exceptions
{
	public class RateLimitException : Exception
	{
		public RateLimitException()
		{ }

		public RateLimitException(string? message)
			: base(message)
		{ }

		public RateLimitException(string? message, Exception? innerException)
			: base(message, innerException)
		{ }
	}
}