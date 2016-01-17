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
    public sealed class PlaylistsFilter : FilterViewModel
    {
        public PlaylistsFilter(ResourceLoader resourceLoader)
            : base(resourceLoader, "PlaylistsItemHeader", "SavedPlaylists")
        {
        }

        private object _items;
        public object Items { get { return _items; } set { _items = value; NotifyPropertyChanged("Items"); } }

        public override void LoadData(MusicLibrary library)
        {
            this.Items = GroupedObservableCollection<char, SavedPlaylist, string>.CreateAlpabeticalGrouping(library.SavedPlaylists, p => p.Name, p => true);
        }
    }
}
