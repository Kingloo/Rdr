using Microsoft.Extensions.Logging;

namespace Rdr
{
	internal static class EventIds
	{
		internal static class App
		{
			internal static readonly EventId StartupStarted = new EventId(100, nameof(StartupStarted));
			internal static readonly EventId StartupFinished = new EventId(101, nameof(StartupStarted));
			internal static readonly EventId UnhandledException = new EventId(102, nameof(UnhandledException));
			internal static readonly EventId UnhandledExceptionEmpty = new EventId(103, nameof(UnhandledExceptionEmpty));
			internal static readonly EventId ExitedNotZero = new EventId(198, nameof(ExitedNotZero));
			internal static readonly EventId Exited = new EventId(199, nameof(Exited));
		}
	}
}