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
		public bool HasEnclosure => !(Enclosure is null);

		public Item(string feedName)
		{
			FeedName = feedName;
		}

		public bool Equals(Item other)
		{
			bool sameName = Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase);
			bool sameLink = AreLinksTheSame(Link, other.Link);

			return sameName && sameLink;
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

			return mine.AbsolutePath.Equals(other.AbsolutePath, StringComparison.OrdinalIgnoreCase);
		}

		public int CompareTo(Item other)
		{
			if (other.Published > Published)
			{
				return 1;
			}
			else if (other.Published < Published)
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
