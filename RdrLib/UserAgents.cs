using System.Linq;

namespace RdrLib.Common
{
	public static class UserAgents
	{
		public const string Firefox_89_Windows = "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0";
		public const string Edge_91_Windows = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.114 Safari/537.36 Edg/91.0.864.59";
		public const string Safari_13_1_MacOSX = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1 Safari/605.1.15";
		public const string Chrome_85_Windows = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/85.0.4183.121 Safari/537.36";
		public const string Opera_66_Windows = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36 OPR/66.0.3515.72";
		public const string Firefox_78ESR_Linux = "Mozilla/5.0 (X11; Linux x86_64; rv:78.0) Gecko/20100101 Firefox/78.0";

		[System.Diagnostics.DebuggerStepThrough]
		public static string GetRandomUserAgent()
		{
			// .TickCount, measured in milliseconds, increments so quickly that the last digit is random enough for our needs

			return System.Environment.TickCount.ToString().Last() switch
			{
				'1' => Firefox_89_Windows,
				'2' => Edge_91_Windows,
				'3' => Safari_13_1_MacOSX,
				'4' => Chrome_85_Windows,
				'5' => Opera_66_Windows,
				_ => Firefox_78ESR_Linux
			};
		}
	}
}
