using System;
using System.Globalization;
using Rdr.Gui;

namespace Rdr
{
	public static class Program
	{
		[STAThread]
		public static int Main()
		{
			int exitCode = 0;

			try
			{
				App app = new App();

				exitCode = app.Run();
			}
#pragma warning disable CA1031 // Do not catch general exception types
			catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
			{
				ExceptionCrashSaverOptions options = new ExceptionCrashSaverOptions(
					DateTimeOffset.Now,
					ex,
					"Rdr",
					Environment.ProcessId,
					CultureInfo.CurrentUICulture
				);

				ExceptionCrashSaver.SaveException(options);

				exitCode = -1;
			}

			return exitCode;
		}
	}
}
