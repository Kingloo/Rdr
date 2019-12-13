using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace RdrLib
{
    public enum DownloadResult
    {
        None,
        NoLink,
        Success,
        Failure,
        Canceled,
        InternetError,
        FileAlreadyExists,
        ErrorWritingFile,
        Interrupted
    }

    public class HeaderException : Exception
    {
        public string UnaddableHeader { get; } = string.Empty;

        public HeaderException()
            : this(string.Empty, new Exception())
        { }

        public HeaderException(string header)
            : this(header, new Exception())
        { }

        public HeaderException(string header, Exception innerException)
            : base(header, innerException)
        {
            UnaddableHeader = header;
        }
    }

    public class DownloadProgress
    {
        public Int64 TotalBytesReceived { get; } = 0L;
        public Int64? ContentLength { get; } = null;

        public DownloadProgress(Int64 totalBytesReceived, Int64? contentLength)
        {
            TotalBytesReceived = totalBytesReceived;
            ContentLength = contentLength;
        }
    }

    internal class Download
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:66.0) Gecko/20100101 Firefox/71.0";

        private static readonly HttpClient client = new HttpClient(
            new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                MaxAutomaticRedirections = 3
            })
        {
            Timeout = TimeSpan.FromSeconds(5d)
        };
        
        public Uri Uri { get; }
        public string Path { get; } = string.Empty;

        static Download()
        {
            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent))
            {
                throw new HeaderException(userAgent);
            }
        }

        internal Download(Uri uri, string path)
        {
            Uri = uri;
            Path = path;
        }

        internal Task<DownloadResult> ToFileAsync() => ToFileAsync(null, CancellationToken.None);

        internal async Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress>? progress, CancellationToken token)
        {
            if (File.Exists(Path)) { return DownloadResult.FileAlreadyExists; }

            int bytesRead = 0;
            Int64 totalBytesReceived = 0L;
            Int64 prevTotalBytesReceived = 0L;
            Int64 reportingThreshold = 1024L * 100L;

            int bytesWrittenThisLoop = 0;
            int throttleThresholdBytes = ((1024 * 1024 * 10) / 8) / 4; // 10 megabits in mebibytes

            Stopwatch stopWatch = Stopwatch.StartNew();
            TimeSpan timeThreshold = TimeSpan.FromMilliseconds(250d);

            byte[] buffer = new byte[1024 * 1024]; // 1 MiB - but bytesRead below only ever seems to return 16384 bytes

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, Uri);
            HttpResponseMessage? response = null;

            Stream? receive = null;
            Stream save = new FileStream(Path, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024 * 50, FileOptions.Asynchronous);

            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false);
                receive = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                Int64? contentLength = response.Content.Headers.ContentLength;

                while ((bytesRead = await receive.ReadAsync(buffer, 0, buffer.Length, token).ConfigureAwait(false)) > 0)
                {
                    totalBytesReceived += bytesRead;
                    bytesWrittenThisLoop += bytesRead;

                    if (bytesWrittenThisLoop > throttleThresholdBytes)
                    {
                        if (stopWatch.Elapsed < timeThreshold)
                        {
                            TimeSpan timeToWait = timeThreshold - stopWatch.Elapsed;

                            await Task.Delay(timeToWait).ConfigureAwait(false);
                        }

                        bytesWrittenThisLoop = 0;
                        stopWatch.Restart();
                    }

                    if (progress != null)
                    {
                        if ((totalBytesReceived - prevTotalBytesReceived) > reportingThreshold)
                        {
                            var downloadProgress = new DownloadProgress(totalBytesReceived, contentLength);

                            progress.Report(downloadProgress);

                            prevTotalBytesReceived = totalBytesReceived;
                        }
                    }
                    
                    await save.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }

                await save.FlushAsync().ConfigureAwait(false);

                return DownloadResult.Success;
            }
            catch (HttpRequestException)    { return DownloadResult.InternetError; }
            catch (IOException)             { return DownloadResult.ErrorWritingFile; }
            catch (TaskCanceledException)   { return DownloadResult.Canceled; }
            finally
            {
                request?.Dispose();
                response?.Dispose();
                receive?.Dispose();
                save?.Dispose();
            }
        }


        internal static Task<(HttpStatusCode, string)> StringAsync(Uri uri) => StringAsync(uri, CancellationToken.None);

        internal static async Task<(HttpStatusCode, string)> StringAsync(Uri uri, CancellationToken token)
        {
            HttpStatusCode status = HttpStatusCode.Unused;
            string text = string.Empty;

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri)
            {
                Version = HttpVersion.Version20
            };

            HttpResponseMessage? response = null;

            try
            {
                using (response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        text = await Task.Run(response.Content.ReadAsStringAsync, token).ConfigureAwait(false);
                    }
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            finally
            {
                request?.Dispose();

                if (response != null)
                {
                    status = response.StatusCode;

                    response.Dispose();
                }
            }

            return (status, text);
        }
    }
}