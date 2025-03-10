using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RdrLib.Model;

namespace RdrLib
{
	public interface IRdrService
	{
		public IReadOnlyCollection<Feed> Feeds { get; }

		bool Add(Feed feed);
		int Add(IEnumerable<Feed> feeds);

		bool Remove(Feed feed);
		int Remove(IEnumerable<Feed> feeds);

		void MarkAsRead(Item item);
		void MarkAsRead(Feed feed);
		void MarkAllAsRead();

		void ClearFeeds();

		Task UpdateAsync(Feed feed);
		Task UpdateAsync(Feed feed, CancellationToken cancellationToken);
		Task UpdateAsync(IEnumerable<Feed> feeds);
		Task UpdateAsync(IEnumerable<Feed> feeds, BatchOptions batchOptions);
		Task UpdateAsync(IEnumerable<Feed> feeds, CancellationToken cancellationToken);
		Task UpdateAsync(IEnumerable<Feed> feeds, BatchOptions batchOptions, CancellationToken cancellationToken);

		Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file);
		Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progress);
		Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, CancellationToken cancellationToken);
		Task<long> DownloadEnclosureAsync(Enclosure enclosure, FileInfo file, IProgress<FileDownloadProgress> progress, CancellationToken cancellationToken);
	}
}
