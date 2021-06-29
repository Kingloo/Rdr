using System;
using System.Diagnostics;
using System.IO;

namespace Rdr.Common
{
	public static class SystemLaunch
	{
		public static bool Path(string path)
		{
			return File.Exists(path) && Launch(path);
		}

		public static bool Uri(Uri uri)
		{
			return uri.IsAbsoluteUri && Launch(uri.AbsoluteUri);
		}

		private static bool Launch(string launchString)
		{
			ProcessStartInfo pInfo = new ProcessStartInfo(launchString)
			{
				UseShellExecute = true
			};

			using Process p = new Process
			{
				StartInfo = pInfo
			};

			return p.Start();
		}
	}
}
