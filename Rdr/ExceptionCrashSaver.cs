using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Rdr
{
	internal record struct ExceptionCrashSaverOptions(
		DateTimeOffset Timestamp,
		Exception Ex,
		string AppName,
		int Pid,
		CultureInfo CultureInfo
	);

	internal static class ExceptionCrashSaver
	{
		private static readonly Encoding _encoding = new UTF8Encoding(
			encoderShouldEmitUTF8Identifier: false,
			throwOnInvalidBytes: false
		);
		
		internal static ExceptionCrashSaverOptions CreateDefaultOptions(Exception ex)
		{
			return new ExceptionCrashSaverOptions(
				DateTimeOffset.Now,
				ex,
				Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown-app",
				Environment.ProcessId,
				CultureInfo.CurrentCulture
			);
		}
		
		internal static void SaveException(Exception ex)
			=> SaveExceptionImpl(CreateDefaultOptions(ex));
		
		internal static void SaveException(ExceptionCrashSaverOptions options)
			=> SaveExceptionImpl(options);

		private static void SaveExceptionImpl(ExceptionCrashSaverOptions options)
		{
			string filename = CreateFilename(options);

			DirectoryInfo directory = CreateDirectory(options);

			FileInfo crashFile = CreateCrashFileInfo(directory, filename);

			string errorMessage = BuildErrorMessage(options);

			File.WriteAllText(crashFile.FullName, errorMessage, _encoding);
		}

		private static string CreateFilename(ExceptionCrashSaverOptions options)
		{
			string appName = options.AppName;
			string exceptionName = options.Ex.GetType().Name;
			string formattedTimestamp = FormatTimestampForFilename(options.Timestamp, options.CultureInfo);

			return $"{appName}-Crash-{exceptionName}--{formattedTimestamp}.txt";
		}

		private static DirectoryInfo CreateDirectory(ExceptionCrashSaverOptions options)
		{
			string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), options.AppName);
			
			DirectoryInfo directoryInfo = new DirectoryInfo(directory);

			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}

			return directoryInfo;
		}

		private static FileInfo CreateCrashFileInfo(DirectoryInfo directoryInfo, string filename)
		{
			string path = Path.Combine(directoryInfo.FullName, filename);

			return new FileInfo(path);
		}

		private static string BuildErrorMessage(ExceptionCrashSaverOptions options)
		{
			string appName = options.AppName;
			string formattedTimestamp = FormatTimestamp(options.Timestamp, options.CultureInfo);
			string exceptionName = options.Ex.GetType().FullName ?? "unknown exception";

			return new StringBuilder()
				.AppendLine(options.CultureInfo, $"[{formattedTimestamp}] {appName} (pid {options.Pid}) crashed because of {exceptionName}")
				.AppendLine(options.Ex.Message)
				.AppendLine(options.Ex.StackTrace)
				.ToString();
		}

		private static string FormatTimestamp(DateTimeOffset timestamp, CultureInfo cultureInfo)
		{
			const string format = "yyyy-MM-ddTHH:mm:ss.fffzzz";

			return timestamp.ToString(format, cultureInfo);
		}
		
		private static string FormatTimestampForFilename(DateTimeOffset timestamp, CultureInfo cultureInfo)
		{
			// filenames can't/shouldn't contains dots (.) or colons (:)

			const string format = "yyyy-MM-ddTHH-mm-ss-fff";

			return timestamp.ToString(format, cultureInfo);
		}
	}
}