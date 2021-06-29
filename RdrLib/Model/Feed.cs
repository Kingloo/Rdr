using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			Link = uri;

			_name = Link.AbsoluteUri;
		}

		public bool Add(Item item)
		{
			if (!_items.Contains(item))
			{
				_items.Add(item);

				return true;
			}

			return false;
		}

		public int AddMany(IEnumerable<Item> items)
		{
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
			if (_items.Contains(item))
			{
				_items.Remove(item);

				return true;
			}

			return false;
		}

		public int RemoveMany(IEnumerable<Item> items)
		{
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

		public bool Equals(Feed other)
		{
			var oic = StringComparison.OrdinalIgnoreCase;

			bool sameHost = Link.DnsSafeHost.Equals(other.Link.DnsSafeHost, oic);
			bool samePathAndQuery = Link.PathAndQuery.Equals(other.Link.PathAndQuery, oic);

			return sameHost && samePathAndQuery;
		}

		public int CompareTo(Feed other) => Name.CompareTo(other.Name);

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(Link.AbsoluteUri);
			sb.AppendLine(Name);
			sb.AppendLine($"Status: {Status.ToString()}");
			sb.AppendLine($"items count: {Items.Count}");

			return sb.ToString();
		}
	}
}
