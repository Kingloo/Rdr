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

		void ClearFeeds();

		Task UpdateAsync(Feed feed);
		Task UpdateAsync(Feed feed, CancellationToken cancellationToken);
		Task UpdateAsync(IEnumerable<Feed> feeds);
		Task UpdateAsync(IEnumerable<Feed> feeds, CancellationToken cancellationToken);

		Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path);
		Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress);
		Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, CancellationToken cancellationToken);
		Task<FileResponse> DownloadEnclosureAsync(Enclosure enclosure, string path, IProgress<FileProgress> progress, CancellationToken cancellationToken);
	}
}
