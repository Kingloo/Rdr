using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RdrLib
{
	public enum Reason
	{
		None,
		Success,
		WebError,
		Timeout,
		FileExists,
		Canceled,
		CompressionError,
		Unknown
	}

	public interface IResponse
	{
		Reason Reason { get; }
		HttpStatusCode? Status { get; set; }
		Exception? Exception { get; set; }
	}

	public class StringResponse : IResponse
	{
		private readonly Uri uri;

		public Reason Reason { get; } = Reason.None;
		public HttpStatusCode? Status { get; set; } = null;
		public Exception? Exception { get; set; } = null;
		public string Text { get; set; } = string.Empty;

		public StringResponse(Uri uri, Reason reason)
		{
			this.uri = uri;
			Reason = reason;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(base.ToString());
			sb.AppendLine(uri.AbsoluteUri);
			sb.AppendLine(Status.HasValue ? Status.Value.ToString() : "no status code");
			sb.AppendLine($"reason: {Reason}");
			sb.AppendLine($"string length: {Text.Length.ToString(CultureInfo.CurrentCulture)}");

			return sb.ToString();
		}
	}

	public class DataResponse : IResponse
	{
		private readonly Uri uri;

		public Reason Reason { get; } = Reason.None;
		public HttpStatusCode? Status { get; set; } = null;
		public Exception? Exception { get; set; } = null;
		public ReadOnlyMemory<byte> Data { get; set; } = new ReadOnlyMemory<byte>();

		public DataResponse(Uri uri, Reason reason)
		{
			this.uri = uri;
			Reason = reason;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(base.ToString());
			sb.AppendLine($"uri: {uri.AbsoluteUri}");
			sb.AppendLine(Status.HasValue ? Status.Value.ToString() : "no status code");
			sb.AppendLine($"reason: {Reason}");
			sb.AppendLine($"data length: {Data.Length.ToString(CultureInfo.CurrentCulture)}");

			return sb.ToString();
		}
	}

	public class FileResponse : IResponse
	{
		private readonly Uri uri;
		private readonly string path;

		public Reason Reason { get; } = Reason.None;
		public HttpStatusCode? Status { get; set; } = null;
		public Exception? Exception { get; set; } = null;

		public FileResponse(Uri uri, string path, Reason reason)
		{
			this.uri = uri;
			this.path = path;
			Reason = reason;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(base.ToString());
			sb.AppendLine($"uri: {uri.AbsoluteUri}");
			sb.AppendLine($"path: {path}");
			sb.AppendLine(Status.HasValue ? Status.Value.ToString() : "no status code");
			sb.AppendLine($"reason: {Reason}");

			return sb.ToString();
		}
	}

	public class FileProgress
	{
		public Int64 BytesWritten { get; } = 0L;
		public Int64 TotalBytesWritten { get; } = 0L;
		public Int64? ContentLength { get; } = -1L;

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
			if (GetDownloadRatio() is decimal ratio)
			{
				return ratio * 100m;
			}
			else
			{
				return null;
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		public string? GetPercentFormatted() => GetPercentFormatted(CultureInfo.CurrentCulture);

		public string? GetPercentFormatted(CultureInfo ci)
		{
			if (GetDownloadRatio() is decimal ratio)
			{
				string percentFormat = GetPercentFormatString(ci);

				return ratio.ToString(percentFormat);
			}
			else
			{
				return null;
			}
		}

		private decimal? GetDownloadRatio()
		{
			if (!ContentLength.HasValue)
			{
				return null;
			}

			return Convert.ToDecimal(TotalBytesWritten) / Convert.ToDecimal(ContentLength.Value);
		}

		private static string GetPercentFormatString(CultureInfo ci)
		{
			string separator = ci.NumberFormat.PercentDecimalSeparator;
			string symbol = ci.NumberFormat.PercentSymbol;

			return string.Format(ci, "0{0}00 {1}", separator, symbol);
		}
	}

	public static class Web
	{
		private static readonly HttpClientHandler handler = new HttpClientHandler
		{
			AllowAutoRedirect = true,
			AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
			MaxAutomaticRedirections = 3,
			SslProtocols = SslProtocols.Tls12
		};

		private static readonly HttpClient client = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(10d)
		};

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<StringResponse> DownloadStringAsync(Uri uri)
			=> DownloadStringAsync(uri, null);

		public static async Task<StringResponse> DownloadStringAsync(Uri uri, Action<HttpRequestMessage>? configureRequest)
		{
			HttpRequestMessage request = new HttpRequestMessage()
			{
				RequestUri = uri
			};

			configureRequest?.Invoke(request);

			HttpResponseMessage? response = null;

			try
			{
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

				return new StringResponse(uri, Reason.Success)
				{
					Status = response.StatusCode,
					Text = text
				};
			}
			catch (HttpRequestException ex)
			{
				return new StringResponse(uri, Reason.WebError)
				{
					Status = response?.StatusCode ?? null,
					Text = ex.Message,
					Exception = ex
				};
			}
			catch (TaskCanceledException ex)
			{
				return new StringResponse(uri, Reason.Timeout)
				{
					Exception = ex
				};
			}
			catch (OperationCanceledException ex)
			{
				return new StringResponse(uri, Reason.WebError)
				{
					Exception = ex
				};
			}
			finally
			{
				request?.Dispose();
				response?.Dispose();
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<DataResponse> DownloadDataAsync(Uri uri)
			=> DownloadDataAsync(uri, null);

		public static async Task<DataResponse> DownloadDataAsync(Uri uri, Action<HttpRequestMessage>? configureRequest)
		{
			HttpRequestMessage request = new HttpRequestMessage()
			{
				RequestUri = uri
			};

			configureRequest?.Invoke(request);

			HttpResponseMessage? response = null;

			try
			{
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				ReadOnlyMemory<byte> data = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

				return new DataResponse(uri, Reason.Success)
				{
					Status = response.StatusCode,
					Data = data
				};
			}
			catch (HttpRequestException ex)
			{
				return new DataResponse(uri, Reason.WebError)
				{
					Status = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (InvalidDataException ex)
			{
				return new DataResponse(uri, Reason.CompressionError)
				{
					Status = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (TaskCanceledException ex)
			{
				return new DataResponse(uri, Reason.Timeout)
				{
					Exception = ex
				};
			}
			catch (OperationCanceledException ex)
			{
				return new DataResponse(uri, Reason.WebError)
				{
					Exception = ex
				};
			}
			finally
			{
				request?.Dispose();
				response?.Dispose();
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(Uri uri, string path)
			=> DownloadFileAsync(uri, path, null, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(Uri uri, string path, Action<HttpRequestMessage> configureRequest)
			=> DownloadFileAsync(uri, path, configureRequest, null, CancellationToken.None);

		[System.Diagnostics.DebuggerStepThrough]
		public static Task<FileResponse> DownloadFileAsync(Uri uri, string path, IProgress<FileProgress> progress)
			=> DownloadFileAsync(uri, path, null, progress, CancellationToken.None);

		public static async Task<FileResponse> DownloadFileAsync(Uri uri, string path, Action<HttpRequestMessage>? configureRequest, IProgress<FileProgress>? progress, CancellationToken token)
		{
			if (String.IsNullOrWhiteSpace(path)) { throw new ArgumentException("path cannot be NullOrWhiteSpace", nameof(path)); }
			if (File.Exists(path)) { return new FileResponse(uri, path, Reason.FileExists); }

			string inProgressPath = GetExtension(path, "inprogress");

			FileResponse? fileResponse = null;

			HttpRequestMessage request = new HttpRequestMessage()
			{
				RequestUri = uri
			};

			configureRequest?.Invoke(request);

			HttpResponseMessage? response = null;

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
				response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);

				response.EnsureSuccessStatusCode();

				Int64? contentLength = response.Content.Headers.ContentLength;

				receive = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

				while ((bytesRead = await receive.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
				{
					await save.WriteAsync(buffer, 0, bytesRead, token).ConfigureAwait(false);

					totalBytesWritten += bytesRead;

					if (progress != null)
					{
						if ((totalBytesWritten - prevTotalBytesWritten) > progressReportThreshold)
						{
							FileProgress fileProgress = new FileProgress(bytesRead, totalBytesWritten, contentLength);

							progress.Report(fileProgress);

							prevTotalBytesWritten = totalBytesWritten;
						}
					}
				}

				await save.FlushAsync().ConfigureAwait(false);

				fileResponse = new FileResponse(uri, path, Reason.Success)
				{
					Status = response.StatusCode
				};
			}
			catch (HttpRequestException ex)
			{
				fileResponse = new FileResponse(uri, path, Reason.WebError)
				{
					Status = response?.StatusCode ?? null,
					Exception = ex
				};
			}
			catch (IOException ex)
			{
				// IOException is thrown by FileStream with FileMode.CreateNew if the file already exists
				// it might be thrown by other things as well
				fileResponse = new FileResponse(uri, path, Reason.FileExists)
				{
					Exception = ex
				};
			}
			catch (TaskCanceledException ex)
			{
				fileResponse = new FileResponse(uri, path, Reason.Canceled)
				{
					Exception = ex
				};
			}
			catch (OperationCanceledException ex)
			{
				fileResponse = new FileResponse(uri, path, Reason.WebError)
				{
					Exception = ex
				};
			}
			finally
			{
				request?.Dispose();
				response?.Dispose();

				receive?.Dispose();
				save?.Dispose();
			}

			// probably unnecessary to wait beyond the finally, but eh, why not?
			await Task.Delay(TimeSpan.FromMilliseconds(150d), token).ConfigureAwait(false);

			string finalPath = fileResponse.Reason switch
			{
				Reason.Success => path,
				Reason.WebError => GetExtension(path, "error"),
				Reason.Canceled => GetExtension(path, "canceled"),
				_ => GetExtension(path, "failed")
			};

			File.Move(inProgressPath, finalPath);

			return fileResponse;
		}

		private static string GetExtension(string path, string extension)
		{
			Directory.CreateDirectory(new FileInfo(path).DirectoryName);

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
}
