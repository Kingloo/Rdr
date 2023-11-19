using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace RdrLib.Model
{
	public class Feed : BindableBase, IEquatable<Feed>, IComparable<Feed>
	{
		public Uri Link { get; }

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value, nameof(Name));
		}

		private FeedStatus _status = FeedStatus.None;
		public FeedStatus Status
		{
			get => _status;
			set => SetProperty(ref _status, value, nameof(Status));
		}

		private readonly ObservableCollection<Item> _items = new ObservableCollection<Item>();
		public IReadOnlyCollection<Item> Items => _items;

		public Feed(Uri uri)
		{
			ArgumentNullException.ThrowIfNull(uri);

			Link = uri;

			_name = Link.AbsoluteUri;
		}

		public bool Add(Item item)
		{
			ArgumentNullException.ThrowIfNull(item);

			if (!_items.Contains(item))
			{
				_items.Add(item);

				return true;
			}

			return false;
		}

		public int AddMany(IEnumerable<Item> items)
		{
			ArgumentNullException.ThrowIfNull(items);

			int added = 0;

			foreach (Item item in items)
			{
				if (Add(item))
				{
					added++;
				}
			}

			return added;
		}

		public bool Remove(Item item)
		{
			ArgumentNullException.ThrowIfNull(item);

			return _items.Remove(item);
		}

		public int RemoveMany(IEnumerable<Item> items)
		{
			ArgumentNullException.ThrowIfNull(items);

			int removed = 0;

			foreach (Item item in items)
			{
				if (Remove(item))
				{
					removed++;
				}
			}

			return removed;
		}

		public bool Equals(Feed? other)
		{
			return other is not null && EqualsInternal(this, other);
		}

		public override bool Equals(object? obj)
		{
			return obj is Feed feed && EqualsInternal(this, feed);
		}

		private static bool EqualsInternal(Feed thisOne, Feed otherOne)
		{
			var oic = StringComparison.OrdinalIgnoreCase;

			bool sameHost = thisOne.Link.DnsSafeHost.Equals(otherOne.Link.DnsSafeHost, oic);
			bool samePathAndQuery = thisOne.Link.PathAndQuery.Equals(otherOne.Link.PathAndQuery, oic);

			return sameHost && samePathAndQuery;
		}

		public static bool operator ==(Feed lhs, Feed rhs)
		{
			if (lhs is null && rhs is null)
			{
				return true;
			}

			if (lhs is null)
			{
				return false;
			}

			if (rhs is null)
			{
				return false;
			}

			return EqualsInternal(lhs, rhs);
		}

		public static bool operator !=(Feed lhs, Feed rhs)
		{
			if (lhs is null && rhs is null)
			{
				return false;
			}

			if (lhs is null)
			{
				return true;
			}

			if (rhs is null)
			{
				return true;
			}

			return !EqualsInternal(lhs, rhs);
		}

		public static bool operator <(Feed lhs, Feed rhs)
		{
			if (lhs is null && rhs is null)
			{
				return false;
			}

			if (lhs is null)
			{
				return true;
			}

			if (rhs is null)
			{
				return false;
			}

			return lhs.CompareTo(rhs) < 0;
		}

		public static bool operator <=(Feed lhs, Feed rhs)
		{
			if (lhs is null && rhs is null)
			{
				return false;
			}

			if (lhs is null)
			{
				return true;
			}

			if (rhs is null)
			{
				return false;
			}

			return lhs.CompareTo(rhs) <= 0;
		}

		public static bool operator >(Feed lhs, Feed rhs)
		{
			if (lhs is null && rhs is null)
			{
				return false;
			}

			if (lhs is null)
			{
				return false;
			}

			if (rhs is null)
			{
				return true;
			}

			return lhs.CompareTo(rhs) > 0;
		}

		public static bool operator >=(Feed lhs, Feed rhs)
		{
			if (lhs is null && rhs is null)
			{
				return false;
			}

			if (lhs is null)
			{
				return false;
			}

			if (rhs is null)
			{
				return true;
			}

			return lhs.CompareTo(rhs) >= 0;
		}

		public override int GetHashCode()
		{
			return Link.GetHashCode();
		}

		public int CompareTo(Feed? other)
		{
			if (other is null)
			{
				return -1;
			}

			return String.CompareOrdinal(Name, other.Name);
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(Link.AbsoluteUri);
			sb.AppendLine(Name);
			sb.AppendLine(CultureInfo.CurrentCulture, $"Status: {Status.ToString()}");
			sb.AppendLine(CultureInfo.CurrentCulture, $"items count: {Items.Count}");

			return sb.ToString();
		}
	}
}
