using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RdrLib.Model;
using RdrLib.Services.Updater;

namespace RdrLib
{
	public interface IRdrService
	{
		bool IsUpdating { get; }
		bool IsDownloading { get; }

		Task<IList<Feed>> LoadAsync(Stream stream, CancellationToken cancellationToken);
		Task<IList<Feed>> LoadAsync(Stream stream, Encoding encoding, CancellationToken cancellationToken);

		Task<FeedUpdateContext> UpdateAsync(Feed feed, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken);
		Task<IList<FeedUpdateContext>> UpdateAsync(IList<Feed> feeds, RdrOptions rdrOptions, bool beConditional, CancellationToken cancellationToken);

		Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, CancellationToken cancellationToken);
		Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progress, CancellationToken cancellationToken);
	}
}
