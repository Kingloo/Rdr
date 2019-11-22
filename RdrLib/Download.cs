using System;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace RdrLib
{
    public enum DownloadResult
    {
        None,
        Success,
        Failure,
        Canceled,
        InternetError,
        FileAlreadyExists,
        InitiationFailed,
        ErrorWritingFile,
        Interrupted
    }

    internal class HeaderException : Exception
    {
        internal string UnaddableHeader { get; } = string.Empty;

        internal HeaderException()
            : this(string.Empty, null)
        { }

        internal HeaderException(string header)
            : this(header, null)
        { }

        internal HeaderException(string header, Exception innerException)
            : base(header, innerException)
        {
            UnaddableHeader = header;
        }
    }

    internal class DownloadProgress
    {
        internal Int64 TotalBytesReceived { get; } = 0L;
        internal Int64? ContentLength { get; } = null;
        internal Uri Uri { get; } = null;
        internal string FilePath { get; } = string.Empty;

        internal DownloadProgress(Uri uri, string filePath, Int64 totalBytesReceived)
            : this(uri, filePath, totalBytesReceived, null)
        { }

        internal DownloadProgress(Uri uri, string filePath, Int64 totalBytesReceived, Int64? contentLength)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            FilePath = filePath;
            TotalBytesReceived = totalBytesReceived;
            ContentLength = contentLength;
        }
    }

    internal class FileDownload : IDisposable
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x66; rv:64.0) Gecko/20100101 Firefox/70.0";

        private readonly Uri uri = null;
        private readonly string path = string.Empty;

        private readonly HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            MaxAutomaticRedirections = 3,
            SslProtocols = SslProtocols.Tls12
        };

        private readonly HttpClient client = null;

        internal FileDownload(Uri uri, string path)
        {
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.path = path ?? throw new ArgumentNullException(nameof(path));

            client = new HttpClient(handler, false)
            {
                Timeout = TimeSpan.FromSeconds(5d)
            };

            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent))
            {
                throw new HeaderException(userAgent);
            }
        }

        internal Task<DownloadResult> ToFileAsync() => ToFileAsync(null, CancellationToken.None);

        internal Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress)
        {
            if (progress is null) { throw new ArgumentNullException(nameof(progress)); }

            return ToFileAsync(progress, CancellationToken.None);
        }

        internal Task<DownloadResult> ToFileAsync(CancellationToken token) => ToFileAsync(null, token);

        internal async Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress, CancellationToken token)
        {
            if (File.Exists(path))
            {
                return DownloadResult.FileAlreadyExists;
            }

            (Stream download, Int64? contentLength) = await SetupStreamAsync(uri).ConfigureAwait(false);

            if (download is null)
            {
                return DownloadResult.InitiationFailed;
            }

            try
            {
                Pipe pipe = new Pipe();

                Task<DownloadResult> downloadFromStream = WriteFromDownloadStreamToPipeAsync(download, pipe.Writer, token);
                Task writeToDisk = ReadFromPipeAndWriteToDiskAsync(path, pipe.Reader, progress, contentLength, token);

                await Task.WhenAll(downloadFromStream, writeToDisk).ConfigureAwait(false);

                return downloadFromStream.IsCompleted ? downloadFromStream.Result : DownloadResult.Failure;
            }
            catch (HttpRequestException)
            {
                return DownloadResult.InternetError;
            }
            catch (IOException)
            {
                return DownloadResult.ErrorWritingFile;
            }
            finally
            {
                download?.Dispose();
            }
        }

        private async Task<(Stream, Int64?)> SetupStreamAsync(Uri uri)
        {
            HttpRequestMessage request = null;

            try
            {
                request = new HttpRequestMessage(HttpMethod.Get, uri);
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                    return (stream, response.Content.Headers.ContentLength);
                }
                else
                {
                    return (null, null);
                }
            }
            catch (HttpRequestException)
            {
                return (null, null);
            }
            catch (TaskCanceledException)
            {
                return (null, null);
            }
            finally
            {
                request?.Dispose();
                
                // don't dispose response here, otherwise the content stream will close as well
            }
        }

        private static async Task<DownloadResult> WriteFromDownloadStreamToPipeAsync(Stream stream, PipeWriter writer, CancellationToken token)
        {
            int BUFSIZE = 1024 * 50;

            while (true)
            {
                Memory<byte> memory = writer.GetMemory(BUFSIZE);

                int bytesRead = await stream.ReadAsync(memory, token).ConfigureAwait(false);

                if (bytesRead < 1)
                {
                    break;
                }

                writer.Advance(bytesRead);

                FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);

                if (flushResult.IsCompleted)
                {
                    break;
                }
            }

            writer.Complete();

            return DownloadResult.Success;
        }

        private async Task ReadFromPipeAndWriteToDiskAsync(string path, PipeReader reader, IProgress<DownloadProgress> progress, Int64? contentLength, CancellationToken token)
        {
            Int64 previousBytesReceived = 0;
            Int64 totalBytesReceived = 0;

            Int64 threshold = 1024 * 100;

            using FileStream fsAsync = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

            while (true)
            {
                ReadResult readResult = await reader.ReadAsync(token).ConfigureAwait(false);

                foreach (ReadOnlyMemory<byte> segment in readResult.Buffer)
                {
                    await fsAsync.WriteAsync(segment, token).ConfigureAwait(false);

                    totalBytesReceived += segment.Length;
                }

                reader.AdvanceTo(readResult.Buffer.End);

                if ((totalBytesReceived - previousBytesReceived) > threshold)
                {
                    if (progress != null)
                    {
                        var downloadProgress = new DownloadProgress(uri, path, totalBytesReceived, contentLength);

                        progress.Report(downloadProgress);
                    }

                    previousBytesReceived = totalBytesReceived;
                }

                if (readResult.IsCompleted)
                {
                    break;
                }
            }

            await fsAsync.FlushAsync().ConfigureAwait(false);

            reader.Complete();
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client?.Dispose();
                    handler?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    internal class TextDownload : IDisposable
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x66; rv:64.0) Gecko/20100101 Firefox/70.0";

        private readonly Uri uri = null;

        private readonly HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            MaxAutomaticRedirections = 3,
            SslProtocols = SslProtocols.Tls12
        };

        private readonly HttpClient client = null;

        internal TextDownload(Uri uri)
        {
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));

            client = new HttpClient(handler, false)
            {
                Timeout = TimeSpan.FromSeconds(5d)
            };

            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent))
            {
                throw new HeaderException(userAgent);
            }
        }

        internal async Task<(HttpStatusCode code, string text)> TextAsync()
        {
            HttpStatusCode result = HttpStatusCode.Unused;
            string text = string.Empty;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            HttpResponseMessage response = null;

            try
            {
                using (response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    result = response.StatusCode;

                    if (response.IsSuccessStatusCode)
                    {
                        text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (HttpRequestException) { }
            finally
            {
                result = response?.StatusCode ?? HttpStatusCode.Unused;

                request?.Dispose();
                response?.Dispose();
            }

            return (result, text);
        }

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    client?.Dispose();
                    handler?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}