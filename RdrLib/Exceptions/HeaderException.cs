using System;
using System.Runtime.Serialization;

namespace RdrLib.Exceptions
{
	public class HeaderException : Exception
	{
		public string UnaddableHeader { get; init; } = string.Empty;

		public HeaderException()
		{ }

		public HeaderException(string? message)
			: base(message)
		{ }

		public HeaderException(string? message, Exception? innerException)
			: base(message, innerException)
		{ }

		protected HeaderException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}
}
