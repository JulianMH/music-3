using MusicConcept.ViewModels.MusicFilters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace MusicConcept.MusicFilters
{
    class FilterViewModelsToPivotItemsConverter : DependencyObject, IValueConverter
    {
        public Frame NavigationFrame
        {
            get { return (Frame)this.GetValue(NavigationFrameProperty); }
            set { this.SetValue(NavigationFrameProperty, value); }
        }

        public static readonly DependencyProperty NavigationFrameProperty = DependencyProperty.Register("NavigationFrame",
            typeof(Frame), typeof(FilterViewModelsToPivotItemsConverter), new PropertyMetadata(null));

        public bool CanChangeSelected
        {
            get { return (bool)this.GetValue(CanChangeSelectedProperty); }
            set { this.SetValue(CanChangeSelectedProperty, value); }
        }

        public static readonly DependencyProperty CanChangeSelectedProperty = DependencyProperty.Register("CanChangeSelected",
            typeof(bool), typeof(FilterViewModelsToPivotItemsConverter), new PropertyMetadata(false));

        public bool GoBackAfterClick
        {
            get { return (bool)this.GetValue(GoBackAfterClickProperty); }
            set { this.SetValue(GoBackAfterClickProperty, value); }
        }

        public static readonly DependencyProperty GoBackAfterClickProperty = DependencyProperty.Register("GoBackAfterClick",
            typeof(bool), typeof(FilterViewModelsToPivotItemsConverter), new PropertyMetadata(false));

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var collection = new ObservableCollection<FilterPivotItem>(((IEnumerable<FilterViewModel>)value).Select<FilterViewModel, FilterPivotItem>(p => CreatePivotItem(p)));

            var notifyCollectionChanged = value as INotifyCollectionChanged;
            if (notifyCollectionChanged != null)
                notifyCollectionChanged.CollectionChanged += (sender, e) =>
                {
                    int index;
                    switch (e.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            index = e.NewStartingIndex;
                            foreach (FilterViewModel newItem in e.NewItems)
                                collection.Insert(index++, CreatePivotItem(newItem));
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            index = e.OldStartingIndex;
                            foreach (FilterViewModel oldItem in e.OldItems)
                                collection.RemoveAt(index++);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                };

            return collection;
        }

        private FilterPivotItem CreatePivotItem(FilterViewModel filterViewModel)
        {
            FilterPivotItem result = null;

            if (filterViewModel.GetType() == typeof(SearchFilter))
                result = new SearchItem() { DataContext = filterViewModel };
            else
                result = new GenericItem() { DataContext = filterViewModel };

            result.Header = filterViewModel.Header;

            result.SetBinding(FilterPivotItem.NavigationFrameProperty, new Binding() { Source = this, Path = new PropertyPath("NavigationFrame") });
            result.SetBinding(FilterPivotItem.CanChangeSelectedProperty, new Binding() { Source = this, Path = new PropertyPath("CanChangeSelected") });
            result.SetBinding(FilterPivotItem.GoBackAfterClickProperty, new Binding() { Source = this, Path = new PropertyPath("GoBackAfterClick") });

            if (result == null)
                throw new NotImplementedException();
            else
                return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new InvalidOperationException();
        }
    }
}
