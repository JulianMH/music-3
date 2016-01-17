using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept
{
    class WhereObservableCollection<T, TSorting> : INotifyCollectionChanged, IEnumerable<T>
    {
        private ObservableCollection<T> resultCollection;
        private ObservableCollection<T> sourceCollection;
        private Func<T, TSorting> getSorting;
        private Func<T, bool> isInList;
        private IComparer<TSorting> comparer;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public WhereObservableCollection(ObservableCollection<T> sourceCollection, Func<T, bool> isInList, Func<T, TSorting> getSorting, IComparer<TSorting> comparer)
        {
            this.sourceCollection = sourceCollection;
            this.getSorting = getSorting;
            this.isInList = isInList;
            this.comparer = comparer;

            Reset();

            this.sourceCollection.CollectionChanged += sourceCollection_CollectionChanged;
        }

        private void Reset()
        {
            if (comparer == null)
                this.resultCollection = new ObservableCollection<T>(sourceCollection.Where(isInList).OrderBy(getSorting));
            else
                this.resultCollection = new ObservableCollection<T>(sourceCollection.Where(isInList).OrderBy(getSorting, comparer));
            this.resultCollection.CollectionChanged += (sender, e) => { if (this.CollectionChanged != null) this.CollectionChanged(this, e); };

            if (this.CollectionChanged != null)
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void AddItems(IList items)
        {
            foreach (object o in items)
            {
                var item = (T)o;
                if (isInList(item))
                    AddSorted(item);
            }
        }

        private void AddSorted(T item)
        {
            var itemCompare = getSorting(item);
            for (int i = 0; i < resultCollection.Count - 1; ++i)
            {
                if ((comparer == null ? ((IComparable)itemCompare).CompareTo(getSorting(resultCollection[i])) : comparer.Compare(itemCompare, getSorting(resultCollection[i]))) < 0)
                {
                    resultCollection.Insert(i, item);
                    return;
                }
            }
            resultCollection.Add(item);
        }

        private void RemoveItems(IList items)
        {
            foreach (object o in items)
            {
                var item = (T)o;
                if (isInList(item))
                    resultCollection.Remove(item);
            }
        }

        void sourceCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.AddItems(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.RemoveItems(e.OldItems);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    this.RemoveItems(e.OldItems);
                    this.AddItems(e.NewItems);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    Reset();
                    break;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return resultCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)resultCollection).GetEnumerator();
        }
    }
}
