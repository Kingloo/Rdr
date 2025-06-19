using System;
using System.Collections;
using RdrLib.Model;

namespace Rdr.Gui
{
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
				return String.CompareOrdinal(x.Name, y.Name);
			}

			bool sortXByName = ShouldSortByName(x.Status);
			bool sortYByName = ShouldSortByName(y.Status);

			if (sortXByName && !sortYByName) { return 1; }
			if (!sortXByName && sortYByName) { return -1; }
			
			return String.CompareOrdinal(x.Name, y.Name);
		}

		private static bool ShouldSortByName(FeedStatus feedStatus)
		{
			return feedStatus == FeedStatus.Ok || feedStatus == FeedStatus.Updating;
		}
	}
}
