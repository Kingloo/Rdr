using System;
using System.Globalization;
using System.Text;

namespace RdrLib.Model
{
    public class Item : BindableBase, IEquatable<Item>, IComparable<Item>
    {
        public Uri Link { get; set; } = null;
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset Published { get; set; } = DateTimeOffset.MinValue;

        private bool _unread = false;
        public bool Unread
        {
            get => _unread;
            set => SetProperty(ref _unread, value, nameof(Unread));
        }
        
        public Enclosure Enclosure { get; set; } = null;
        public bool HasEnclosure => !(Enclosure is null);

        public Item() { }

        public bool Equals(Item other)
        {
            if (other is null) { return false; }

            return Link.AbsolutePath.Equals(other.Link.AbsolutePath);
        }

        public int CompareTo(Item other)
        {
            if (other is null) { throw new ArgumentNullException(nameof(other)); }

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

            sb.AppendLine(Link.AbsoluteUri);
            sb.AppendLine(Name);
            sb.AppendLine(Published.ToString(CultureInfo.CurrentCulture));
            sb.AppendLine(Unread.ToString());
            sb.AppendLine(HasEnclosure ? Enclosure.ToString() : "no enclosure");

            return sb.ToString();
        }
    }
}
