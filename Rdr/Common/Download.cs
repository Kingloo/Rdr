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
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:60.0) Gecko/20100101 Firefox/60.0";
        
        private static HttpClient client = null;

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
                // TODO
                //
                // this eventuality should never happen
                // therefore it should throw a new custom exception instead

                string message = string.Format(
                    CultureInfo.CurrentCulture,
                    "User-Agent ({0}) could not be added",
                    userAgent);

                Log.LogMessage(message);
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
            => ToFileAsync(null, CancellationToken.None);

        public Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress)
            => ToFileAsync(progress, CancellationToken.None);

        public async Task<DownloadResult> ToFileAsync(IProgress<DownloadProgress> progress, CancellationToken token)
        {
            if (File.Exists) { return DownloadResult.FileAlreadyExists; }

            InitClient();

            var request = new HttpRequestMessage(HttpMethod.Get, Uri);
            
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

            byte[] buffer = new byte[1024 * 1024 * 15]; // 15 MiB

            try
            {
                while ((bytesRead = await receive.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
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

                    // does this help reduce bandwidth hogging ?
                    // 500 ms caused significant download slowdown
                    // 50 ms was better but still pretty slow
                    // 5 ms seems to work
                    await Task.Delay(5);
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
            }

            return DownloadResult.Success;
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
