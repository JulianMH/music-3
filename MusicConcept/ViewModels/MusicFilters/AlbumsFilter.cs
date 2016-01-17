using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace MusicConcept.ViewModels.MusicFilters
{
    public sealed class AlbumsFilter : FilterViewModel
    {
        public AlbumsFilter(ResourceLoader resourceLoader)
            : base(resourceLoader, "AlbumsItemHeader", "Albums")
        {
        }

        private object _items;
        public object Items { get { return _items; } set { _items = value; NotifyPropertyChanged("Items"); } }

        public override void LoadData(MusicLibrary library)
        {
            this.Items = GroupedObservableCollection<char, Album, string>.CreateAlpabeticalGrouping(library.Albums, p => p.Name, p => true);
        }
    }
}
