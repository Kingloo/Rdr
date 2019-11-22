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
        FileAlreadyExists,
        InitiationFailed,
        Interrupted
    }

    public class HeaderException : Exception
    {
        public string UnaddableHeader { get; } = string.Empty;

        public HeaderException()
            : this(string.Empty, null)
        { }

        public HeaderException(string header)
            : this(header, null)
        { }

        public HeaderException(string header, Exception innerException)
            : base(header, innerException)
        {
            UnaddableHeader = header;
        }
    }

    public class DownloadProgress
    {
        public Int64 BytesRead { get; } = 0L;
        public Int64 TotalBytesReceived { get; } = 0L;
        public Int64? ContentLength { get; } = null;
        public Uri Uri { get; } = null;
        public string FilePath { get; } = string.Empty;

        public DownloadProgress(Uri uri, string filePath, Int64 bytesRead, Int64 totalBytesReceived)
            : this(uri, filePath, bytesRead, totalBytesReceived, null)
        { }

        public DownloadProgress(Uri uri, string filePath, Int64 bytesRead, Int64 totalBytesReceived, Int64? contentLength)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            FilePath = filePath;
            BytesRead = bytesRead;
            TotalBytesReceived = totalBytesReceived;
            ContentLength = contentLength;
        }
    }

    public class Download : IDisposable
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x66; rv:64.0) Gecko/20100101 Firefox/70.0";

        private readonly Uri uri = null;
        private readonly string path = string.Empty;

        private static readonly HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            MaxAutomaticRedirections = 3,
            SslProtocols = SslProtocols.Tls12
        };

        private readonly HttpClient client = new HttpClient(handler, false)
        {
            Timeout = TimeSpan.FromSeconds(7d)
        };

        public Download(Uri uri, string path)
        {
            this.uri = uri ?? throw new ArgumentNullException(nameof(uri));
            this.path = path ?? throw new ArgumentNullException(nameof(path));

            SetClientHeader();
        }

        private void SetClientHeader()
        {
            client.DefaultRequestHeaders.Clear();

            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent))
            {
                throw new HeaderException(userAgent);
            }
        }

        public async Task<(HttpStatusCode code, string text)> TextAsync(Uri uri)
        {
            HttpStatusCode result = HttpStatusCode.Unused;
            string text = string.Empty;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);

            try
            {
                using (HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
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
                request.Dispose();
            }

            return (result, text);
        }

        public Task<DownloadResult> ToFileAsync() => ToFileAsync(null, CancellationToken.None);

        public Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress)
        {
            if (progress is null) { throw new ArgumentNullException(nameof(progress)); }

            return ToFileAsync(progress, CancellationToken.None);
        }

        public Task<DownloadResult> ToFileAsync(CancellationToken token) => ToFileAsync(null, token);

        public async Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress, CancellationToken token)
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

            Pipe pipe = new Pipe();

            Task<DownloadResult> downloadFromStream = WriteFromDownloadStreamToPipeAsync(download, pipe.Writer, token);
            Task writeToDisk = ReadFromPipeAndWriteToDiskAsync(path, pipe.Reader, progress, contentLength, token);

            await Task.WhenAll(downloadFromStream, writeToDisk).ConfigureAwait(false);

            return downloadFromStream.IsCompleted ? downloadFromStream.Result : DownloadResult.Failure;
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

            try
            {
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
                
                return DownloadResult.Success;
            }
            catch (HttpRequestException)
            {
                return DownloadResult.Interrupted;
            }
            finally
            {
                writer.Complete();
            }
        }

        private async Task ReadFromPipeAndWriteToDiskAsync(string path, PipeReader reader, IProgress<DownloadProgress> progress, Int64? contentLength, CancellationToken token)
        {
            FileStream fsAsync = null;

            Int64 previousBytesReceived = 0;
            Int64 totalBytesReceived = 0;

            Int64 threshold = 1024 * 100;

            try
            {
                fsAsync = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);

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
                            var downloadProgress = new DownloadProgress(uri, path, readResult.Buffer.Length, totalBytesReceived, contentLength);

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
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (IOException)
            {
                return;
            }
            finally
            {
                fsAsync?.Dispose();

                reader.Complete();
            }
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