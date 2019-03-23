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
        InternetError
    }

    public class HeaderException : Exception
    {
        public string UnaddableHeader { get; } = string.Empty;

        public HeaderException(string header)
            : base($"unaddable header: {header}")
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

    public class Download
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:66.0) Gecko/20100101 Firefox/66.0";
        
        private static HttpClient client = null;

        private CancellationTokenSource cts = null;

        private static void InitClient()
        {
            if (client != null) { return; }

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                MaxAutomaticRedirections = 3
            };

            client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(5d)
            };

            if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent))
            {
                throw new HeaderException(userAgent);
            }
        }

        #region Properties
        public Uri Uri { get; } = null;
        public FileInfo File { get; } = null;
        #endregion

        public Download(Uri uri, FileInfo file)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            File = file ?? throw new ArgumentNullException(nameof(file));
        }

        public Task<DownloadResult> ToFileAsync()
            => ToFileAsync(null);

        public async Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress)
        {
            if (File.Exists) { return DownloadResult.FileAlreadyExists; }

            InitClient();

            cts = new CancellationTokenSource();

            int bytesRead = 0;
            Int64 totalBytesReceived = 0L;
            Int64 prevTotalBytesReceived = 0L;
            Int64 reportingThreshold = 1024L * 333L; // 333 KiB

            byte[] buffer = new byte[1024 * 1024]; // 1 MiB - but bytesRead below is only ever 16384 bytes

            HttpRequestMessage request = null;
            HttpResponseMessage response = null;

            Stream receive = null;
            Stream save = null;

            try
            {
                request = new HttpRequestMessage(HttpMethod.Get, Uri);

                response = await client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                    .ConfigureAwait(false);

                receive = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                save = new FileStream(
                    File.FullName,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    1024 * 1024 * 15, // 15 MiB
                    FileOptions.Asynchronous);

                Int64? contentLength = response.Content.Headers.ContentLength;

                while ((bytesRead = await receive.ReadAsync(buffer, 0, buffer.Length, cts.Token).ConfigureAwait(false)) > 0)
                {
                    totalBytesReceived += bytesRead;

                    if ((totalBytesReceived - prevTotalBytesReceived) > reportingThreshold)
                    {
                        var dlProgress = new DownloadProgress(
                            request.RequestUri,
                            File.FullName,
                            bytesRead,
                            totalBytesReceived,
                            contentLength);

                        progress.Report(dlProgress);

                        prevTotalBytesReceived = totalBytesReceived;
                    }

                    await save.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                }

                await save.FlushAsync().ConfigureAwait(false);
            }
            catch (HttpRequestException)
            {
                return DownloadResult.InternetError;
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
                cts?.Dispose();

                cts = null;
            }

            return DownloadResult.Success;
        }

        public void Cancel()
        {
            cts?.Cancel();
        }


        public static Task<string> WebsiteAsync(Uri uri)
            => WebsiteAsync(uri, CancellationToken.None);

        public static Task<string> WebsiteAsync(Uri uri, CancellationToken token)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            
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
            InitClient();

            string text = string.Empty;

            try
            {
                var httpOption = HttpCompletionOption.ResponseHeadersRead;

                using (var response = await client.SendAsync(request, httpOption, token).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        text = await Task.Run(response.Content.ReadAsStringAsync, token).ConfigureAwait(false);
                    }
                    else
                    {
                        var cc = CultureInfo.CurrentCulture;
                        string link = request.RequestUri.AbsoluteUri;
                        HttpStatusCode httpCode = response.StatusCode;

                        string message = string.Format(cc, "downloading {0} failed: {1}", link, httpCode);

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
                var cc = CultureInfo.CurrentCulture;
                string link = request.RequestUri.AbsoluteUri;

                string message = string.Format(cc, "user cancelled download of {0}", link);

                await Log.LogMessageAsync(message).ConfigureAwait(false);
            }

            return text;
        }
    }
}
