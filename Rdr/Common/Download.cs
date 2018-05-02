using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Rdr.Common
{
    public enum DownloadResult
    {
        None,
        Success,
        Failure,
        Canceled,
        FileAlreadyExists,
        WebError
    }

    public class DownloadProgress
    {
        private readonly Int64 _bytesRead = 0L;
        public Int64 BytesRead => _bytesRead;

        private readonly Int64 _totalBytesReceived = 0L;
        public Int64 TotalBytesReceived => _totalBytesReceived;

        private readonly Int64? _contentLength = null;
        public Int64? ContentLength => _contentLength;

        private readonly Uri _uri = null;
        public Uri Uri => _uri;

        private readonly string _filePath = string.Empty;
        public string FilePath => _filePath;

        public DownloadProgress(Uri uri, string filePath, Int64 bytesRead, Int64 totalBytesReceived)
            : this(uri, filePath, bytesRead, totalBytesReceived, null)
        { }

        public DownloadProgress(Uri uri, string filePath, Int64 bytesRead, Int64 totalBytesReceived, Int64? contentLength)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _filePath = filePath;
            _bytesRead = bytesRead;
            _totalBytesReceived = totalBytesReceived;
            _contentLength = contentLength;
        }
    }

    public class Download
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:59.0) Gecko/20100101 Firefox/59.0";

        private static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            MaxAutomaticRedirections = 3
        };

        private static HttpClient client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(5d)
        };


        #region Properties
        private readonly Uri _uri = default;
        public Uri Uri => _uri;

        private readonly FileInfo _file = default;
        public FileInfo File => _file;
        #endregion

        public Download(Uri uri, FileInfo file)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }

        public Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress)
            => ToFileAsync(progress, CancellationToken.None);

        public async Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress, CancellationToken token)
        {
            if (File.Exists) { return DownloadResult.FileAlreadyExists; }

            var request = new HttpRequestMessage(HttpMethod.Get, Uri);
            
            if (!request.Headers.UserAgent.TryParseAdd(userAgent))
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    "User-Agent ({0}) could not be added",
                    userAgent);

                await Log.LogMessageAsync(message).ConfigureAwait(false);
            }

            var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token)
                .ConfigureAwait(false);

            Stream receive = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            Stream save = new FileStream(
                File.FullName,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                1024 * 1024 * 5, // 5 MiB
                FileOptions.Asynchronous);

            Int64? contentLength = response.Content.Headers.ContentLength;

            int bytesRead = 0;
            Int64 totalBytesReceived = 0L;
            Int64 prevTotalBytesReceived = 0L;
            Int64 reportingThreshold = 1024 * 333; // 333 KiB

            byte[] buffer = new byte[1024 * 1024 * 5]; // 5 MiB

            try
            {
                while ((bytesRead = await receive.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    totalBytesReceived += bytesRead;

                    if ((totalBytesReceived - prevTotalBytesReceived) > reportingThreshold)
                    {
                        progress.Report(
                            new DownloadProgress(
                                request.RequestUri,
                                File.FullName,
                                bytesRead,
                                totalBytesReceived,
                                contentLength));

                        prevTotalBytesReceived = totalBytesReceived;
                    }

                    await save.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }

                await save.FlushAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                return DownloadResult.WebError;
            }
            catch (IOException)
            {
                return DownloadResult.Failure;
            }
            catch (TaskCanceledException)
            {
                return DownloadResult.Canceled;
            }
            finally
            {
                request?.Dispose();
                response?.Dispose();
                receive?.Dispose();
                save?.Dispose();
            }

            return DownloadResult.Success;
        }


        public static Task<string> WebsiteAsync(Uri uri)
            => WebsiteAsync(uri, CancellationToken.None);

        public static Task<string> WebsiteAsync(Uri uri, CancellationToken token)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            
            if (!request.Headers.UserAgent.TryParseAdd(userAgent))
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    "User-Agent ({0}) could not be added",
                    userAgent);

                Log.LogMessage(message);
            }

            return DownloadStringAsync(request, token);
        }

        public static Task<string> WebsiteAsync(HttpRequestMessage request)
            => WebsiteAsync(request, CancellationToken.None);

        public static Task<string> WebsiteAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }

            return DownloadStringAsync(request, token);
        }


        private static async Task<string> DownloadStringAsync(HttpRequestMessage request, CancellationToken token)
        {
            string text = string.Empty;

            try
            {
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        text = await Task.Run(response.Content.ReadAsStringAsync, token).ConfigureAwait(false);
                    }
                    else
                    {
                        string message = string.Format(
                            CultureInfo.CurrentCulture,
                            "downloading {0}: {1}",
                            request.RequestUri.AbsoluteUri,
                            response.StatusCode);

                        await Log.LogMessageAsync(message).ConfigureAwait(false);
                    }
                }
            }
            catch (HttpRequestException) { }
            catch (TaskCanceledException) { }
            finally
            {
                request?.Dispose();
            }

            if (token.IsCancellationRequested)
            {
                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    "user cancelled download of {0}",
                    request.RequestUri.AbsoluteUri);

                await Log.LogMessageAsync(message).ConfigureAwait(false);
            }

            return text;
        }
    }
}
