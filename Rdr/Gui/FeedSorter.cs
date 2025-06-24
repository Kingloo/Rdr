using System;
using System.Collections;
using System.Runtime.CompilerServices;
using RdrLib.Model;

namespace Rdr.Gui
{
	/// <summary>
	/// Sort by name if FeedStatus is either Ok or Updating. Sorts feeds earlier when FeedStatus is anything else. Earlier is expressed by -1.
	/// </summary>
	internal sealed class FeedSorter : IComparer
	{
		internal FeedSorter() { }

		public int Compare(object? x, object? y)
		{
			Feed? feedX = x as Feed;
			Feed? feedY = y as Feed;

			if (feedX is null && feedY is null)
			{
				return 0;
			}

			if (feedX is null)
			{
				return feedY is null ? 0 : 1;
			}

			if (feedY is null)
			{
				return -1;
			}

			return CompareInternal(feedX, feedY);
		}

		private static int CompareInternal(Feed x, Feed y)
		{
			if (x.Status == y.Status)
			{
				return CompareByName(x, y);
			}

			bool sortXByName = ShouldSortByName(x.Status);
			bool sortYByName = ShouldSortByName(y.Status);

			if (sortXByName && !sortYByName) { return 1; }
			if (!sortXByName && sortYByName) { return -1; }

			return CompareByName(x, y);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool ShouldSortByName(FeedStatus feedStatus)
		{
			return feedStatus == FeedStatus.Ok || feedStatus == FeedStatus.Updating;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int CompareByName(Feed x, Feed y)
		{
			return String.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase);
		}
	}
}
