using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RdrLib.Exceptions;

namespace RdrLib
{
	public enum Reason
	{
		None,
		Success,
		ETagMatch,
		Failed,
		FileExists,
		Canceled,
		Unknown
	}

	public interface IResponse
	{
		Reason Reason { get; }
		HttpStatusCode? StatusCode { get; init; }
		Exception? Exception { get; init; }
	}

	public class StringResponse : IResponse
	{
		private readonly Uri uri;

		public Reason Reason { get; } = Reason.None;
		public HttpStatusCode? StatusCode { get; init; } = null;
		public Exception? Exception { get; init; } = null;
		public string Text { get; init; } = string.Empty;

		public StringResponse(Uri uri, Reason reason)
		{
			ArgumentNullException.ThrowIfNull(uri);

			this.uri = uri;
			Reason = reason;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(base.ToString());
			sb.AppendLine(uri.AbsoluteUri);
			sb.AppendLine(StatusCode.HasValue ? StatusCode.Value.ToString() : "no status code");
			sb.AppendLine(CultureInfo.CurrentCulture, $"reason: {Reason}");
			sb.AppendLine(CultureInfo.CurrentCulture, $"string length: {Text.Length.ToString(CultureInfo.CurrentCulture)}");

			return sb.ToString();
		}
	}

	public class DataResponse : IResponse
	{
		private readonly Uri uri;

		public Reason Reason { get; } = Reason.None;
		public HttpStatusCode? StatusCode { get; init; } = null;
		public Exception? Exception { get; init; } = null;
		public ReadOnlyMemory<byte> Data { get; init; } = ReadOnlyMemory<byte>.Empty;

		public DataResponse(Uri uri, Reason reason)
		{
			ArgumentNullException.ThrowIfNull(uri);

			this.uri = uri;
			Reason = reason;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(base.ToString());
			sb.AppendLine(CultureInfo.CurrentCulture, $"uri: {uri.AbsoluteUri}");
			sb.AppendLine(StatusCode.HasValue ? StatusCode.Value.ToString() : "no status code");
			sb.AppendLine(CultureInfo.CurrentCulture, $"reason: {Reason}");
			sb.AppendLine(CultureInfo.CurrentCulture, $"data length: {Data.Length.ToString(CultureInfo.CurrentCulture)}");

			return sb.ToString();
		}
	}

	public class FileResponse : IResponse
	{
		private readonly Uri uri;
		private readonly string path;

		public Reason Reason { get; } = Reason.None;
		public HttpStatusCode? StatusCode { get; init; } = null;
		public Exception? Exception { get; init; } = null;

		public FileResponse(Uri uri, string path, Reason reason)
		{
			ArgumentNullException.ThrowIfNull(uri);
			ArgumentNullException.ThrowIfNull(path);

			this.uri = uri;
			this.path = path;

			Reason = reason;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(base.ToString());
			sb.AppendLine(CultureInfo.CurrentCulture, $"uri: {uri.AbsoluteUri}");
			sb.AppendLine(CultureInfo.CurrentCulture, $"path: {path}");
			sb.AppendLine(StatusCode.HasValue ? StatusCode.Value.ToString() : "no status code");
			sb.AppendLine(CultureInfo.CurrentCulture, $"reason: {Reason}");

			return sb.ToString();
		}
	}

	public class FileProgress
	{
		public Int64 BytesWritten { get; } = 0L;
		public Int64 TotalBytesWritten { get; } = 0L;
		public Int64? ContentLength { get; } = null;

		public FileProgress(Int64 bytesWritten, Int64 totalBytesWritten)
			: this(bytesWritten, totalBytesWritten, null)
		{ }

		public FileProgress(Int64 bytesWritten, Int64 totalBytesWritten, Int64? contentLength)
		{
			BytesWritten = bytesWritten;
			TotalBytesWritten = totalBytesWritten;
			ContentLength = contentLength;
		}

		public decimal? GetPercent()
		{
			return GetDownloadRatio() switch
			{
				decimal ratio => ratio * 100m,
				_ => null
			};
		}

		public string? GetPercentFormatted()
			=> GetPercentFormatted(CultureInfo.CurrentCulture);

		public string? GetPercentFormatted(CultureInfo cultureInfo)
		{
			ArgumentNullException.ThrowIfNull(cultureInfo);

			return GetDownloadRatio() switch
			{
				decimal ratio => ratio.ToString(GetPercentFormatString(cultureInfo), cultureInfo),
				_ => null
			};
		}

		private decimal? GetDownloadRatio()
		{
			return ContentLength.HasValue switch
			{
				true => Convert.ToDecimal(TotalBytesWritten) / Convert.ToDecimal(ContentLength.Value),
				false => null
			};
		}

		private static string GetPercentFormatString(CultureInfo cultureInfo)
		{
			return string.Format(
				cultureInfo,
				"0{0}00 {1}",
				cultureInfo.NumberFormat.PercentDecimalSeparator,
				cultureInfo.NumberFormat.PercentSymbol);
		}
	}

#pragma warning disable CA1724
	public static class Web
#pragma warning restore CA1724
	{
		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<StringResponse> DownloadStringAsync(HttpClient client, Uri uri)
			=> DownloadStringAsyncInternal(client, uri, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<StringResponse> DownloadStringAsync(HttpClient client, Uri uri, CancellationToken cancellationToken)
			=> DownloadStringAsyncInternal(client, uri, null, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<StringResponse> DownloadStringAsync(HttpClient client, Uri uri, Action<HttpRequestMessage> configureRequest)
			=> DownloadStringAsyncInternal(client, uri, configureRequest, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<StringResponse> DownloadStringAsync(HttpClient client, Uri uri, Action<HttpRequestMessage> configureRequest, CancellationToken cancellationToken)
			=> DownloadStringAsyncInternal(client, uri, configureRequest, cancellationToken);

		private static async ValueTask<StringResponse> DownloadStringAsyncInternal(HttpClient client, Uri uri, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(client);
			ArgumentNullException.ThrowIfNull(uri);

			HttpRequestMessage request = new HttpRequestMessage()
			{
				RequestUri = uri
			};

			configureRequest?.Invoke(request);

			HttpResponseMessage? response = null;
			StringResponse? stringResponse = null;

			try
			{
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				if (DoesResponseHaveETag(response, out ETag? currentEtag))
				{
					if (ETagCache.ETags.TryGetValue(uri, out ETag? retrievedETag))
					{
						if (retrievedETag == currentEtag)
						{
							return new StringResponse(uri, Reason.ETagMatch);
						}
						else
						{
							ETagCache.ETags.TryUpdate(uri, currentEtag, retrievedETag);
						}
					}
					else
					{
						if (!ETagCache.ETags.TryAdd(uri, currentEtag))
						{
							throw new ETagException($"failed to add ETag '{currentEtag.Value}'");
						}
					}
				}

				string text = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

				stringResponse = new StringResponse(uri, Reason.Success)
				{
					StatusCode = response.StatusCode,
					Text = text
				};
			}
			catch (HttpRequestException ex)
			{
				stringResponse = new StringResponse(uri, Reason.Failed)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (HttpIOException ex)
			{
				stringResponse = new StringResponse(uri, Reason.Failed)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (OperationCanceledException ex)
			{
				stringResponse = new StringResponse(uri, Reason.Canceled)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			finally
			{
				request?.Dispose();
				response?.Dispose();
			}

			return stringResponse;
		}

		private static bool DoesResponseHaveETag(HttpResponseMessage response, [NotNullWhen(true)] out ETag? eTag)
		{
			if (response.Headers?.ETag?.Tag is string eTagValue)
			{
				if (!String.IsNullOrEmpty(eTagValue))
				{
					eTag = new ETag(eTagValue);

					return true;
				}
			}
			
			eTag = null;

			return false;
		}

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<DataResponse> DownloadDataAsync(HttpClient client, Uri uri)
			=> DownloadDataAsyncInternal(client, uri, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<DataResponse> DownloadDataAsync(HttpClient client, Uri uri, CancellationToken cancellationToken)
			=> DownloadDataAsyncInternal(client, uri, null, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<DataResponse> DownloadDataAsync(HttpClient client, Uri uri, Action<HttpRequestMessage> configureRequest)
			=> DownloadDataAsyncInternal(client, uri, configureRequest, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static ValueTask<DataResponse> DownloadDataAsync(HttpClient client, Uri uri, Action<HttpRequestMessage> configureRequest, CancellationToken cancellationToken)
			=> DownloadDataAsyncInternal(client, uri, configureRequest, cancellationToken);

		private static async ValueTask<DataResponse> DownloadDataAsyncInternal(HttpClient client, Uri uri, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(client);
			ArgumentNullException.ThrowIfNull(uri);

			HttpRequestMessage request = new HttpRequestMessage()
			{
				RequestUri = uri
			};

			configureRequest?.Invoke(request);

			HttpResponseMessage? response = null;
			DataResponse? dataResponse = null;

			try
			{
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				ReadOnlyMemory<byte> data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);

				dataResponse = new DataResponse(uri, Reason.Success)
				{
					StatusCode = response.StatusCode,
					Data = data
				};
			}
			catch (HttpRequestException ex)
			{
				dataResponse = new DataResponse(uri, Reason.Failed)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (HttpIOException ex)
			{
				dataResponse = new DataResponse(uri, Reason.Failed)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (OperationCanceledException ex)
			{
				dataResponse = new DataResponse(uri, Reason.Canceled)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			finally
			{
				request?.Dispose();
				response?.Dispose();
			}

			return dataResponse;
		}

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path)
			=> DownloadFileAsyncInternal(client, uri, path, null, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, CancellationToken cancellationToken)
			=> DownloadFileAsyncInternal(client, uri, path, null, null, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, Action<HttpRequestMessage> configureRequest)
			=> DownloadFileAsyncInternal(client, uri, path, configureRequest, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, Action<HttpRequestMessage> configureRequest, CancellationToken cancellationToken)
			=> DownloadFileAsyncInternal(client, uri, path, configureRequest, null, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, IProgress<FileProgress> progress)
			=> DownloadFileAsyncInternal(client, uri, path, null, progress, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, IProgress<FileProgress> progress, CancellationToken cancellationToken)
			=> DownloadFileAsyncInternal(client, uri, path, null, progress, cancellationToken);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, Action<HttpRequestMessage> configureRequest, IProgress<FileProgress> progress)
			=> DownloadFileAsyncInternal(client, uri, path, configureRequest, progress, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(HttpClient client, Uri uri, string path, Action<HttpRequestMessage> configureRequest, IProgress<FileProgress> progress, CancellationToken cancellationToken)
			=> DownloadFileAsyncInternal(client, uri, path, configureRequest, progress, cancellationToken);

		private static async Task<FileResponse> DownloadFileAsyncInternal(
			HttpClient client,
			Uri uri,
			string path,
			Action<HttpRequestMessage>? configureRequest,
			IProgress<FileProgress>? progress,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(client);
			ArgumentNullException.ThrowIfNull(uri);
			ArgumentNullException.ThrowIfNull(path);

			if (File.Exists(path))
			{
				return new FileResponse(uri, path, Reason.FileExists);
			}

			string inProgressPath = GetExtension(path, "inprogress");

			HttpRequestMessage request = new HttpRequestMessage()
			{
				RequestUri = uri
			};

			configureRequest?.Invoke(request);

			HttpResponseMessage? response = null;
			FileResponse? fileResponse = null;

			int receiveBufferSize = 1024 * 1024; // 1024 * 1024 = 1 MiB
			int saveBufferSize = 4096 * 16;

			Stream? receive = null;
			Stream? save = new FileStream(inProgressPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, saveBufferSize, FileOptions.Asynchronous);

			int bytesRead = 0;
			byte[] buffer = new byte[receiveBufferSize];
			Int64 totalBytesWritten = 0L;
			Int64 prevTotalBytesWritten = 0L;
			Int64 progressReportThreshold = 1024L * 100L; // 1024 * 100 = 100 KiB

			try
			{
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				Int64? contentLength = response.Content.Headers.ContentLength;

				receive = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

#pragma warning disable CA1835
				while ((bytesRead = await receive.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
				{
					await save.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
#pragma warning restore CA1835

					totalBytesWritten += bytesRead;

					if (progress is not null)
					{
						if ((totalBytesWritten - prevTotalBytesWritten) > progressReportThreshold)
						{
							FileProgress fileProgress = new FileProgress(bytesRead, totalBytesWritten, contentLength);

							progress.Report(fileProgress);

							prevTotalBytesWritten = totalBytesWritten;
						}
					}
				}

				await save.FlushAsync(CancellationToken.None).ConfigureAwait(false);

				fileResponse = new FileResponse(uri, path, Reason.Success)
				{
					StatusCode = response.StatusCode
				};
			}
			catch (HttpRequestException ex)
			{
				fileResponse = new FileResponse(uri, path, Reason.Failed)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (HttpIOException ex)
			{
				fileResponse = new FileResponse(uri, path, Reason.Failed)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (IOException ex)
			{
				// IOException is thrown by FileStream with FileMode.CreateNew if the file already exists
				// it might be thrown by other things as well
				fileResponse = new FileResponse(uri, path, Reason.FileExists)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (OperationCanceledException ex)
			{
				fileResponse = new FileResponse(uri, path, Reason.Canceled)
				{
					StatusCode = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			finally
			{
				request?.Dispose();
				response?.Dispose();

				if (receive is not null)
				{
					await receive.DisposeAsync().ConfigureAwait(false);
				}

				await save.DisposeAsync().ConfigureAwait(false);
			}

			// probably unnecessary to wait beyond the finally, but eh, why not?
			await Task.Delay(TimeSpan.FromMilliseconds(100d), CancellationToken.None).ConfigureAwait(false);

			string finalPath = fileResponse.Reason switch
			{
				Reason.Success => path,
				Reason.Canceled => GetExtension(path, "canceled"),
				_ => GetExtension(path, "failed")
			};

			File.Move(inProgressPath, finalPath);

			return fileResponse;
		}

		private static string GetExtension(string path, string extension)
		{
			Directory.CreateDirectory(new FileInfo(path).DirectoryName ?? string.Empty);

			string newPath = $"{path}.{extension}";

			if (!File.Exists(newPath))
			{
				return newPath;
			}

			for (int i = 1; i < 100; i++)
			{
				string nextAttemptedPath = $"{newPath}{i}";

				if (!File.Exists(nextAttemptedPath))
				{
					return nextAttemptedPath;
				}
			}

			return $"{path}{Guid.NewGuid()}";
		}
	}

	internal sealed record ETag(string Value);

	internal static class ETagCache
	{
		internal static readonly ConcurrentDictionary<Uri, ETag> ETags = new ConcurrentDictionary<Uri, ETag>();
	}
}
