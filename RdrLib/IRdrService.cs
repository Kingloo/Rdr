using System;
using System.Collections.Generic;
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

		void Clear();

		ValueTask UpdateAsync(Feed feed);
		ValueTask UpdateAsync(Feed feed, CancellationToken cancellationToken);
		ValueTask UpdateAsync(IEnumerable<Feed> feeds);
		ValueTask UpdateAsync(IEnumerable<Feed> feeds, CancellationToken cancellationToken);

		ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path);
		ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress);
		ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, CancellationToken cancellationToken);
		ValueTask<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress, CancellationToken cancellationToken);
	}
}
