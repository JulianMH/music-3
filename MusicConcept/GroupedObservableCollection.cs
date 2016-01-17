using MusicConcept.Library;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept
{
    public class GroupedObservableCollection<TKey, TValue, TSorting> : NotifyPropertyChangedObject, INotifyCollectionChanged, IEnumerable<IGrouping<TKey, TValue>>
    {
        private ObservableCollection<Grouping<TKey, TValue>> groupedCollection;
        private ObservableCollection<TValue> sourceCollection;
        private IEnumerable<TKey> defaultGroups;
        private Func<TValue, TKey> getGroup;
        private Func<TValue, TSorting> getSorting;
        private Func<TValue, bool> isInList;
        private IComparer<TSorting> comparer;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private bool _isEmpty;
        public bool IsEmpty { get { return _isEmpty; } private set { bool oldValue = _isEmpty; _isEmpty = value; if (oldValue != _isEmpty) NotifyPropertyChanged("IsEmpty"); } }

        private bool _isLessThanTwoGroups;
        public bool IsLessThanTwoGroups { get { return _isLessThanTwoGroups; } private set { bool oldValue = _isLessThanTwoGroups; _isLessThanTwoGroups = value; if (oldValue != _isLessThanTwoGroups) NotifyPropertyChanged("IsLessThanTwoGroups"); } }

        public GroupedObservableCollection(ObservableCollection<TValue> sourceCollection, Func<TValue, TKey> getGroup, 
            Func<TValue, TSorting> getSorting, Func<TValue, bool> isInList,
            IEnumerable<TKey> defaultGroups, IComparer<TSorting> comparer)
        {
            this.sourceCollection = sourceCollection;
            this.defaultGroups = defaultGroups;
            this.getGroup = getGroup;
            this.getSorting = getSorting;
            this.isInList = isInList;
            this.comparer = comparer;

            ResetGrouping();

            this._isEmpty = !this.groupedCollection.Any();
            this._isLessThanTwoGroups = this.groupedCollection.Count < 2;
            this.sourceCollection.CollectionChanged += sourceCollection_CollectionChanged;
        }

        private void ResetGrouping()
        {
            var groupedItems = 
                sourceCollection.Where(isInList).OrderBy(getSorting, comparer).GroupBy(getGroup);

            this.groupedCollection = new ObservableCollection<Grouping<TKey, TValue>>(defaultGroups != null ?
                defaultGroups.Select(p => new Grouping<TKey, TValue>(p, groupedItems.FirstOrDefault(q => q.Key.Equals(p)))) :
                groupedItems.Select(p => new Grouping<TKey, TValue>(p.Key, p)));

            this.groupedCollection.CollectionChanged += (sender, e) => { if (this.CollectionChanged != null) this.CollectionChanged(this, e); };

            if (this.CollectionChanged != null)
                this.CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void AddItems(IList items)
        {
            foreach (object o in items)
            {
                var item = (TValue)o;
                if(isInList(item))
                {
                    var groupKey = getGroup(item);
                    var group = this.groupedCollection.FirstOrDefault(p => p.Key.Equals(groupKey));
                    if (group == null)
                        this.groupedCollection.Add(new Grouping<TKey, TValue>(groupKey, new TValue[] { item }));
                    else
                        AddSorted(item, group);
                }
            }
        }

        private void AddSorted(TValue item, Grouping<TKey, TValue> group)
        {
            var itemCompare = getSorting(item);
            for (int i = 0; i < group.Count - 1; ++i)
            {
                if ((comparer == null ? ((IComparable)itemCompare).CompareTo(getSorting(group[i])) : comparer.Compare(itemCompare, getSorting(group[i]))) < 0)
                {
                    group.Insert(i, item);
                    return;
                }
            }
            group.Add(item);
        }

        private void RemoveItems(IList items)
        {
            foreach (object o in items)
            {
                var item = (TValue)o;
                if (isInList(item))
                {
                    var groupKey = getGroup(item);
                    var group = this.groupedCollection.FirstOrDefault(p => p.Key.Equals(groupKey));

                    if (group == null)
                        throw new InvalidOperationException();
                    else
                    {
                        group.Remove(item);
                        if (group.Count == 0 && defaultGroups != null && !defaultGroups.Contains(group.Key))
                            this.groupedCollection.Remove(group);
                    }
                }
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
                    ResetGrouping();
                    break;
            }

            this.IsEmpty = !groupedCollection.Any();
            this.IsLessThanTwoGroups = this.groupedCollection.Count < 2;
        }

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            return groupedCollection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)groupedCollection).GetEnumerator();
        }

        public static GroupedObservableCollection<char, TValue, string> CreateAlpabeticalGrouping(ObservableCollection<TValue> items, Func<TValue, string> getGroupingElement, Func<TValue, bool> isInList)
        {
            string alphabet = ApplicationSettings.SortingAlphabet.Read();

            return new GroupedObservableCollection<char, TValue, string>(items, p => SortingHelper.GetAlphabetGroup(getGroupingElement(p), alphabet), getGroupingElement, isInList, alphabet, StringComparer.Ordinal);
        }
    }
}
