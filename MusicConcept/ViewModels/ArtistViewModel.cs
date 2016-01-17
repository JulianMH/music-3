using MusicConcept.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicConcept.ViewModels
{
    public class ArtistViewModel : NotifyPropertyChangedObject
    {
        public string Name { get; private set; }

        private object _songs;
        public object Songs { get { return _songs; } set { _songs = value; NotifyPropertyChanged("Songs"); } }
        private object _albums;
        public object Albums { get { return _albums; } set { _albums = value; NotifyPropertyChanged("Albums"); } }

        private MusicLibrary library;

        public ArtistViewModel(MusicLibrary library, string name)
        {
            this.Name = name;
            this.library = library;
            library.PropertyChanged += library_PropertyChanged;

            if (library.Songs != null) LoadSongs();
            if (library.Albums != null) LoadAlbums();
        }

        void library_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Songs") LoadSongs();
            if (e.PropertyName == "Albums") LoadAlbums();
        }

        private void LoadAlbums()
        {
            this.Albums = new WhereObservableCollection<Album, string>(library.Albums, p => p.Artist == this.Name, p => p.Name, StringComparer.Ordinal);
        }

        private void LoadSongs()
        {
            this.Songs = GroupedObservableCollection<char, Song, string>.CreateAlpabeticalGrouping(library.Songs, p => p.Name, p => StringComparer.CurrentCultureIgnoreCase.Equals(p.Artist, this.Name));
        }
    }
}
