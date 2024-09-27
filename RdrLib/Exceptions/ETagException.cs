using System;

namespace RdrLib.Exceptions
{
	public class ETagException : Exception
	{
		public ETagException() { }

		public ETagException(string message)
			: base(message)
		{ }

		public ETagException(string message, Exception innerException)
			: base(message, innerException)
		{ }
	}
}
