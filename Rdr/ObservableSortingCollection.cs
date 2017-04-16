using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace Rdr
{
    public interface IAlternativeSort
    {
        int SortId { get; set; }
    }

    public sealed class ObservableSortingCollection<T> : ObservableCollection<T> where T : IAlternativeSort, IComparable<T>
    {
        #region Properties
        private T _sortFirst = default(T);
        public T SortFirst
        {
            get => _sortFirst;
            set
            {
                _sortFirst = value;
                _sortFirst.SortId = Int32.MinValue;
            }
        }

        private T _sortLast = default(T);
        public T SortLast
        {
            get => _sortLast;
            set
            {
                _sortLast = value;
                _sortLast.SortId = Int32.MaxValue;
            }
        }
        #endregion

        #region Ctors
        public ObservableSortingCollection(T sortFirst)
        {
            //CollectionChanged += WhenCollectionChanged;
            //PropertyChanged += WhenPropertyChanged;

            SortFirst = sortFirst;
            Add(sortFirst);
        }
        
        public ObservableSortingCollection(T sortFirst, T sortLast)
            : this(sortFirst)
        {
            SortLast = sortLast;
            Add(sortLast);
        }
        #endregion
        
        public void DoSorting()
        {
            var all = Items
                .Where(x => x.SortId > Int32.MinValue && x.SortId < Int32.MaxValue)
                .ToList();

            all.Sort();
            
            foreach (T each in all)
            {
                each.SortId = all.IndexOf(each);
            }
        }
    }
}
