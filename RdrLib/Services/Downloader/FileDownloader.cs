using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using RdrLib.Model;

namespace RdrLib.Services.Downloader
{
	public class FileDownloader
	{
		private readonly IHttpClientFactory httpClientFactory;

		public bool IsDownloading { get; private set; } = false;

		public FileDownloader(IHttpClientFactory httpClientFactory)
		{
			ArgumentNullException.ThrowIfNull(httpClientFactory);

			this.httpClientFactory = httpClientFactory;
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

			using ResponseSet headerResponseSet = await Web2.PerformHeaderRequest(httpClient, enclosure.Link, cancellationToken).ConfigureAwait(false);

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
