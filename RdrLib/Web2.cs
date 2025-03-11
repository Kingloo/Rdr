using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RdrLib
{
	public record class FileDownloadProgress(int BytesRead, long TotalBytesWritten, long? ContentLength);
	public record class ETag2(string Value);
	public record class RetryHeaderWithTimestamp(DateTimeOffset Time, RetryConditionHeaderValue? RetryHeader);

	public static class Web2
	{	
		[System.Diagnostics.DebuggerStepThrough]
		public static Task<ResponseSet> PerformHeaderRequest(HttpClient client, Uri uri, CancellationToken cancellationToken)
			=> PerformHeaderRequestInternal(client, uri, null, cancellationToken);
		
		[System.Diagnostics.DebuggerStepThrough]
		public static Task<ResponseSet> PerformHeaderRequest(HttpClient client, Uri uri, Action<HttpRequestMessage> configureRequest, CancellationToken cancellationToken)
			=> PerformHeaderRequestInternal(client, uri, configureRequest, cancellationToken);

		private static async Task<ResponseSet> PerformHeaderRequestInternal(HttpClient client, Uri uri, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(client);
			ArgumentNullException.ThrowIfNull(uri);
			
			ResponseSet responseSet = new ResponseSet(uri);

			Uri uriToVisit = uri;
			bool shouldContinue = true;
			
			while (shouldContinue)
			{
				using HttpRequestMessage request = new HttpRequestMessage
				{
					RequestUri = uriToVisit
				};

				configureRequest?.Invoke(request);

				HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

#pragma warning disable CA2000 // Dispose objects before losing scope
// disposing 'ResponseSet' disposes each 'ResponseSetItem's
				responseSet.Responses.Add(new ResponseSetItem(uri, response));
#pragma warning restore CA2000 // Dispose objects before losing scope

				if (!response.IsSuccessStatusCode && response.Headers.Location is Uri nextUri)
				{
					uriToVisit = nextUri;
					
					shouldContinue = true;
				}
				else
				{
					shouldContinue = false;
				}
			}

			return responseSet;
		}

		public static Task<string> PerformBodyRequestToString(HttpResponseMessage response, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(response);

			return response.Content.ReadAsStringAsync(cancellationToken);
		}

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<long> PerformBodyRequestToFile(HttpResponseMessage response, FileInfo file, CancellationToken cancellationToken)
			=> PerformBodyRequestToFileInternal(response, file, null, cancellationToken);
		
		[System.Diagnostics.DebuggerStepThrough]
		public static Task<long> PerformBodyRequestToFile(HttpResponseMessage response, FileInfo file, IProgress<FileDownloadProgress> progress, CancellationToken cancellationToken)
			=> PerformBodyRequestToFileInternal(response, file, progress, cancellationToken);
		
		private static async Task<long> PerformBodyRequestToFileInternal(HttpResponseMessage response, FileInfo file, IProgress<FileDownloadProgress>? progress, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(response);
			ArgumentNullException.ThrowIfNull(file);
			ArgumentNullException.ThrowIfNull(progress);

			string inProgressPath = GetInProgressPath(file);

			Stream readStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
			Stream writeStream = new FileStream(inProgressPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 4096, FileOptions.Asynchronous);

			byte[] buffer = new byte[1024 * 512];
			
			int bytesRead = 0;
			long totalBytesRead = 0;
			long previousTotalBytesRead = 0;
			long? contentLength = response.Content.Headers.ContentLength;
			
			long progressReportThreshold = contentLength.HasValue && contentLength.Value > 1024 * 1024 * 50 // 50 MiB
				? 1024 * 100 // 100 KiB
				: 16384;
			
			try
			{
				while ((bytesRead = await readStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
				{
					await writeStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

					totalBytesRead += bytesRead;

					if (progress is not null && totalBytesRead - previousTotalBytesRead > progressReportThreshold)
					{
						progress.Report(new FileDownloadProgress(bytesRead, totalBytesRead, contentLength));

						previousTotalBytesRead = totalBytesRead;
					}
				}

				await writeStream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
			}
			finally
			{
				await readStream.DisposeAsync().ConfigureAwait(false);
				await writeStream.DisposeAsync().ConfigureAwait(false);
			}

			await Task.Delay(TimeSpan.FromMilliseconds(50d), CancellationToken.None).ConfigureAwait(false);

			File.Move(inProgressPath, file.FullName, overwrite: false);
			
			return totalBytesRead;
		}

		private static string GetInProgressPath(FileInfo file)
		{
			string tempExtension = $"{file.Extension}.{Guid.NewGuid().ToString()[..6]}-inprogress";
			return Path.ChangeExtension(file.FullName, tempExtension);
		}

		public static bool HasETagMatch(HttpResponseMessage response, ETag2 previousETag)
		{
			ArgumentNullException.ThrowIfNull(response);
			ArgumentNullException.ThrowIfNull(previousETag);

			return response.Headers?.ETag?.Tag is string currentETag && currentETag == previousETag.Value;
		}

		public static TimeSpan GetAmountOfTimeLeftOnRateLimit(RetryConditionHeaderValue retryHeader, DateTimeOffset now)
		{
			ArgumentNullException.ThrowIfNull(retryHeader);

			DateTimeOffset expiration = DateTimeOffset.MinValue;

			if (retryHeader.Delta is TimeSpan delta)
			{
				return delta;
			}

			if (retryHeader.Date is DateTimeOffset future)
			{
				return future > now ? future - now : TimeSpan.Zero;
			}

			return TimeSpan.Zero;
		}
		
		public static TimeSpan GetAmountOfTimeLeftOnRateLimit(RetryConditionHeaderValue retryHeader, DateTimeOffset now, DateTimeOffset before)
		{
			ArgumentNullException.ThrowIfNull(retryHeader);
			
			DateTimeOffset expiration = DateTimeOffset.MinValue;

			if (retryHeader.Delta is TimeSpan delta)
			{
				expiration = before + delta;
			}

			if (retryHeader.Date is DateTimeOffset)
			{
				expiration = retryHeader.Date.Value;
			}

			return expiration > now
				? (expiration - now)
				: TimeSpan.Zero;
		}
	}

	public sealed class ResponseSet : IDisposable
	{
		public Uri InitialUri { get; init; }
		public IList<ResponseSetItem> Responses { get; } = new List<ResponseSetItem>();
		
		public ResponseSet(Uri initialUri)
		{
			ArgumentNullException.ThrowIfNull(initialUri);
			
			InitialUri = initialUri;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			if (Responses.Count > 0)
			{
				_ = sb.AppendLine("----------- start -----------");
				
				_ = sb.AppendLine(InitialUri.AbsoluteUri);

				foreach (ResponseSetItem responseSetItem in Responses)
				{
					_ = sb.Append(responseSetItem.ToString());
				}

				_ = sb.AppendLine("----------- end -----------");
			}
			else
			{
				_ = sb.AppendLine("no responses");
			}

			return sb.ToString();
		}

		private bool disposedValue = false;

		public void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					foreach (ResponseSetItem stackItem in Responses)
					{
						stackItem.Dispose();
					}
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);

			GC.SuppressFinalize(this);
		}
	}

	public class ResponseSetItem : IDisposable
	{
		public Uri Uri { get; init; }
		public HttpResponseMessage Response { get; init; }

		public ResponseSetItem(Uri uri, HttpResponseMessage response)
		{
			ArgumentNullException.ThrowIfNull(uri);
			ArgumentNullException.ThrowIfNull(response);

			Uri = uri;
			Response = response;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			foreach (KeyValuePair<string, IEnumerable<string>> each in Response.Headers)
			{
				_ = sb.AppendLine(CultureInfo.InvariantCulture, $"{each.Key}: {String.Join(" | ", each.Value)}");
			}

			return sb.ToString();
		}

		private bool disposedValue = false;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					Response.Dispose();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);

			GC.SuppressFinalize(this);
		}
	}	
}
