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
			App app = new App();

			int exitCode = app.Run();

			if (exitCode != 0)
			{
				string message = String.Format(CultureInfo.CurrentCulture, "Rdr exited with code {0}", exitCode);

				Console.Error.WriteLine(message);
			}

			return exitCode;
		}
	}
}
