using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.ViewModels
{
    public class AlbumViewModel : NotifyPropertyChangedObject
    {
        private object _songs;
        public object Songs { get { return _songs; } set { _songs = value; NotifyPropertyChanged("Songs"); } }

        public Album Album { get; private set; }

        MusicLibrary library;

        public AlbumViewModel(MusicLibrary library, Album album)
        {
            this.Album = album;
            this.library = library;
            library.PropertyChanged += library_PropertyChanged;

            if (library.Songs != null) LoadSongs();
        }

        void library_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Songs") LoadSongs();
        }

        private void LoadSongs()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;

            this.Songs = new GroupedObservableCollection<uint, Song, uint>(library.Songs,
                  p => p.AlbumCDNumber,
                  p => (p.AlbumCDNumber * 10000) + p.AlbumTrackNumber,
                  p => comparer.Equals(p.AlbumArtist, Album.Artist) && comparer.Equals(p.Album, Album.Name),
                  null, null);
        }
    }
}
