using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RdrLib.Model;

namespace RdrLib.Services.Downloader
{
	public class FileDownloader
	{
		private readonly IHttpClientFactory httpClientFactory;
		private readonly IOptionsMonitor<RdrOptions> rdrOptionsMonitor;

		public bool IsDownloading { get; private set; } = false;

		public FileDownloader(IHttpClientFactory httpClientFactory, IOptionsMonitor<RdrOptions> rdrOptionsMonitor)
		{
			ArgumentNullException.ThrowIfNull(httpClientFactory);
			ArgumentNullException.ThrowIfNull(rdrOptionsMonitor);

			this.httpClientFactory = httpClientFactory;
			this.rdrOptionsMonitor = rdrOptionsMonitor;
		}

		public Task<long> DownloadAsync(Enclosure enclosure, FileInfo file, CancellationToken cancellationToken)
			=> DownloadAsyncInternal(enclosure, file, null, cancellationToken);

		public Task<long> DownloadAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progressReport, CancellationToken cancellationToken)
			=> DownloadAsyncInternal(enclosure, file, progressReport, cancellationToken);

		private async Task<long> DownloadAsyncInternal(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress>? progressReport, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(enclosure);
			ArgumentNullException.ThrowIfNull(file);

			using HttpClient httpClient = httpClientFactory.CreateClient();

			void configureRequest(HttpRequestMessage requestMessage)
			{
				if (rdrOptionsMonitor
					.CurrentValue
					.DomainSpecificUserAgents
					.FirstOrDefault(custom => enclosure.Link.DnsSafeHost.Contains(custom.Domain, StringComparison.OrdinalIgnoreCase))
					is CustomUserAgent customUserAgent)
				{
					requestMessage.Headers.UserAgent.Clear();
					requestMessage.Headers.UserAgent.ParseAdd(customUserAgent.UserAgent);
				}
			}

			using ResponseSet headerResponseSet = await Web2.PerformHeaderRequest(httpClient, enclosure.Link, configureRequest, cancellationToken).ConfigureAwait(false);

			if (headerResponseSet.Responses.LastOrDefault() is ResponseSetItem lastResponseSetItem)
			{
				IsDownloading = true;

				try
				{
					if (progressReport is not null)
					{
						return await Web2.PerformBodyRequestToFile(lastResponseSetItem.Response, file, progressReport, cancellationToken).ConfigureAwait(false);
					}
					else
					{
						return await Web2.PerformBodyRequestToFile(lastResponseSetItem.Response, file, cancellationToken).ConfigureAwait(false);
					}
				}
				finally
				{
					IsDownloading = false;
				}
			}

			return -1L;
		}
	}
}
