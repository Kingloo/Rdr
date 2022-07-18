using System;
using System.Globalization;
using System.Text;

namespace RdrLib.Model
{
	public class Item : BindableBase, IEquatable<Item>, IComparable<Item>
	{
		public Uri? Link { get; set; } = null;
		public string Name { get; set; } = string.Empty;
		public string FeedName { get; set; } = string.Empty;

		private DateTimeOffset _published = DateTimeOffset.MinValue;
		public DateTimeOffset Published
		{
			get => _published.ToLocalTime();
			set => _published = value;
		}

		private bool _unread = true;
		public bool Unread
		{
			get => _unread;
			set => SetProperty(ref _unread, value, nameof(Unread));
		}

		public Enclosure? Enclosure { get; set; } = null;
		public bool HasEnclosure { get => Enclosure is not null; }

		public Item(string feedName)
		{
			if (String.IsNullOrWhiteSpace(feedName))
			{
				throw new ArgumentNullException(nameof(feedName), "feed name cannot be null-or-whitespace");
			}

			FeedName = feedName;
		}

		public bool Equals(Item? other)
		{
			return other is not null && EqualsInternal(this, other);
		}

		public override bool Equals(object? obj)
		{
			return obj is Item item && EqualsInternal(this, item);
		}

		private static bool EqualsInternal(Item thisOne, Item otherOne)
		{
            if (AreLinksTheSame(thisOne.Link, otherOne.Link))
            {
                return true;
            }
            
            bool sameName = thisOne.Name.Equals(otherOne.Name, StringComparison.OrdinalIgnoreCase);

            return sameName;
		}

		public static bool operator ==(Item lhs, Item rhs)
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

		public static bool operator !=(Item lhs, Item rhs)
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

		public static bool operator <(Item lhs, Item rhs)
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

		public static bool operator <=(Item lhs, Item rhs)
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

		public static bool operator >(Item lhs, Item rhs)
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

		public static bool operator >=(Item lhs, Item rhs)
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
			return FeedName.GetHashCode(StringComparison.Ordinal);
		}

		private static bool AreLinksTheSame(Uri? mine, Uri? other)
		{
			if (mine is null && other is null)
			{
				return true;
			}

			if ((mine is null) != (other is null))
			{
				return false;
			}

			if (mine is null)
			{
				return false;
			}

			if (other is null)
			{
				return false;
			}

            return mine.PathAndQuery.Equals(other.PathAndQuery, StringComparison.OrdinalIgnoreCase);
		}

		public int CompareTo(Item? other)
		{
			if (other?.Published > Published)
			{
				return 1;
			}
			else if (other?.Published < Published)
			{
				return -1;
			}
			else
			{
				return 0;
			}
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();

			sb.AppendLine(Link?.AbsoluteUri ?? "no link");
			sb.AppendLine(Name);
			sb.AppendLine(Published.ToString(CultureInfo.CurrentCulture));
			sb.AppendLine(Unread.ToString());
			sb.AppendLine(Enclosure?.ToString() ?? "no enclosure");

			return sb.ToString();
		}
	}
}
