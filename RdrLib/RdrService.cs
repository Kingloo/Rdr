using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RdrLib.Model;
using RdrLib.Services.Loader;
using RdrLib.Services.Updater;
using RdrLib.Services.Downloader;

namespace RdrLib
{
	public class RdrService : IRdrService
	{
		private readonly FeedLoader feedLoader;
		private readonly FeedUpdater feedUpdater;
		private readonly FileDownloader fileDownloader;

		public bool IsUpdating { get => feedUpdater.IsUpdating; }
		public bool IsDownloading { get => fileDownloader.IsDownloading; }

		public RdrService(
			FeedLoader feedLoader,
			FeedUpdater feedUpdater,
			FileDownloader fileDownloader
		)
		{
			ArgumentNullException.ThrowIfNull(feedLoader);
			ArgumentNullException.ThrowIfNull(feedUpdater);
			ArgumentNullException.ThrowIfNull(fileDownloader);

			this.feedLoader = feedLoader;
			this.feedUpdater = feedUpdater;
			this.fileDownloader = fileDownloader;
		}

		public Task<IList<Feed>> LoadAsync(Stream stream, CancellationToken cancellationToken)
			=> LoadAsyncInternal(stream, FeedLoader.DefaultEncoding, cancellationToken);

		public Task<IList<Feed>> LoadAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken)
			=> LoadAsyncInternal(stream, encoding, cancellationToken);

		private Task<IList<Feed>> LoadAsyncInternal(Stream stream, Encoding encoding, CancellationToken cancellationToken)
		{
			return feedLoader.LoadAsync(stream, encoding, cancellationToken);
		}

		public async Task<FeedUpdateContext> UpdateAsync(Feed feed, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			IList<FeedUpdateContext> feedUpdateContext = await UpdateAsyncInternal(
				new List<Feed>(capacity: 1) { feed },
				rdrOptions,
				beConditional,
				cancellationToken)
			.ConfigureAwait(false);

			return feedUpdateContext[0];
		}

		public Task<IList<FeedUpdateContext>> UpdateAsync(IList<Feed> feeds, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
			=> UpdateAsyncInternal(feeds, rdrOptions, beConditional, cancellationToken);

		private Task<IList<FeedUpdateContext>> UpdateAsyncInternal(IList<Feed> feeds, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken)
		{
			return feedUpdater.UpdateAsync(feeds, rdrOptions, beConditional, cancellationToken);
		}

		public Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, file, null, cancellationToken);

		public Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progress, CancellationToken cancellationToken)
			=> DownloadEnclosureAsyncInternal(enclosure, file, progress, cancellationToken);

		private Task<long> DownloadEnclosureAsyncInternal(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress>? progress, CancellationToken cancellationToken)
		{
			return progress is not null
				? fileDownloader.DownloadAsync(enclosure, file, progress, cancellationToken)
				: fileDownloader.DownloadAsync(enclosure, file, cancellationToken);
		}
	}
}
