using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace RdrLib.Model
{
    public class Feed : BindableBase, IEquatable<Feed>, IComparable<Feed>
    {
        public Uri Link { get; } = null;

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
            Link = uri ?? throw new ArgumentNullException(nameof(uri));
        }

        public void Add(Item item)
        {
            if (item is null) { throw new ArgumentNullException(nameof(item)); }

            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        public void AddMany(IEnumerable<Item> items)
        {
            if (items is null) { throw new ArgumentNullException(nameof(items)); }

            foreach (Item item in items)
            {
                Add(item);
            }
        }

        public void Remove(Item item)
        {
            if (item is null) { throw new ArgumentNullException(nameof(item)); }

            if (_items.Contains(item))
            {
                _items.Remove(item);
            }
        }

        public void RemoveMany(IEnumerable<Item> items)
        {
            if (items is null) { throw new ArgumentNullException(nameof(items)); }

            foreach (Item item in items)
            {
                Remove(item);
            }
        }

        public bool Equals(Feed other)
        {
            if (other is null) { return false; }

            return Link.AbsolutePath.Equals(other.Link.AbsolutePath);
        }

        public int CompareTo(Feed other)
        {
            if (other is null) { throw new ArgumentNullException(nameof(other)); }

            return Name.CompareTo(other.Name);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(Link.AbsoluteUri);
            sb.AppendLine(Name);
            sb.AppendLine(Status.ToString());
            sb.AppendLine($"items count: {Items.Count}");

            return sb.ToString();
        }
    }
}
