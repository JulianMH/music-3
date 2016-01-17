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
    public sealed class AllSongsFilter : FilterViewModel
    {
        private Func<Song, bool> includeInList;

        public AllSongsFilter(ResourceLoader resourceLoader, Func<Song, bool> includeInList, SongSource songSource)
            : base(resourceLoader, "AllSongsItemHeader", "Songs")
        {
            this.includeInList = includeInList ?? ((s) => true);
            this.SongSource = songSource;
        }

        private object _items;
        public object Items { get { return _items; } set { _items = value; NotifyPropertyChanged("Items"); } }

        public override void LoadData(MusicLibrary library)
        {
            this.Items = GroupedObservableCollection<char, Song, string>.CreateAlpabeticalGrouping(library.Songs, p => p.Name, includeInList);
        }
    }
}
