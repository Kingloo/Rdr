using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Rdr.Common
{
    public enum DownloadResult
    {
        None = 0,
        Success = 1,
        Failure = 2,
        HttpError = 3,
        FileError = 4,
        FileAlreadyExists = 5
    }
    
    public class Download
    {
        private const string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:58.0) Gecko/20100101 Firefox/58.0";

        private static HttpClientHandler handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            MaxAutomaticRedirections = 3
        };

        private static HttpClient client = new HttpClient(handler, false)
        {
            Timeout = TimeSpan.FromSeconds(20d)
        };

        public static Task<string> WebsiteAsync(Uri uri)
        {
            if (uri == null) { throw new ArgumentNullException(nameof(uri)); }

            return WebsiteAsyncImpl(new HttpRequestMessage(HttpMethod.Get, uri));
        }

        public static Task<string> WebsiteAsync(HttpRequestMessage request)
        {
            if (request == null) { throw new ArgumentNullException(nameof(request)); }

            return WebsiteAsyncImpl(request);
        }

        private static async Task<string> WebsiteAsyncImpl(HttpRequestMessage request)
        {
            request.Headers.Add("User-Agent", userAgent);
            
            string text = string.Empty;

            try
            {
                using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                await Log.LogExceptionAsync(ex, request.RequestUri.AbsoluteUri).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { }

            return text;
        }
        

        #region Properties
        private readonly Uri _uri = null;
        public Uri SourceUri => _uri;

        private readonly FileInfo _file = null;
        public FileInfo TargetFile => _file;
        #endregion
        
        public Download(Uri uri, FileInfo file)
        {
            _uri = uri ?? throw new ArgumentNullException(nameof(uri));
            _file = file ?? throw new ArgumentNullException(nameof(file));
        }
        
        public async Task<DownloadResult> ToFileAsync(bool deleteOnFailure, IProgress<decimal> progress)
        {
            if (TargetFile.Exists) { return DownloadResult.FileAlreadyExists; }

            HttpResponseMessage response = default;
            FileStream fsAsync = default;

            try
            {
                response = await client.GetAsync(SourceUri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode) { return DownloadResult.HttpError; }

                int bytesRead = 0;
                int totalBytesRead = 0;
                decimal length = Convert.ToDecimal(response.Content.Headers.ContentLength);
                decimal percent = 0m;
                byte[] buffer = new byte[1024 * 1024 * 1]; // 1 MiB

                fsAsync = new FileStream(
                    _file.FullName,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    4096,
                    FileOptions.Asynchronous);

                using (Stream input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    while ((bytesRead = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        percent = totalBytesRead / length;

                        progress.Report(percent);

                        totalBytesRead += bytesRead;

                        await fsAsync.WriteAsync(buffer, 0, bytesRead).ConfigureAwait(false);
                    }

                    await fsAsync.FlushAsync().ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                await Log.LogExceptionAsync(ex).ConfigureAwait(false);
            }
            catch (IOException)
            {
                if (deleteOnFailure && File.Exists(_file.FullName))
                {
                    File.Delete(_file.FullName);
                }

                return DownloadResult.Failure;
            }
            finally
            {
                fsAsync?.Dispose();
                response?.Dispose();
            }

            return DownloadResult.Success;
        }
    }
}
